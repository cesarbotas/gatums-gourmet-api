using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Estoque;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Entities.Vendas;
using GatumsGourmet.Domain.Enums.Estoque;
using GatumsGourmet.Domain.Enums.Pedidos;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V1.Pedidos.CheckoutPedido;

public sealed class CheckoutPedidoHandler(
    ICarrinhoRepository carrinho,
    IPedidoRepository pedidos,
    IVendaRepository vendas,
    IProdutoRepository produtos,
    IMovimentoEstoqueRepository movimentos,
    IClock clock,
    ILogger<CheckoutPedidoHandler> logger) : ICheckoutPedidoHandler
{
    public async Task<Result<CheckoutPedidoResponse>> Handle(Guid usuarioId, string clienteNome, CancellationToken ct = default)
    {
        var itensCarrinho = await carrinho.GetByUsuarioAsync(usuarioId, ct);
        if (itensCarrinho.Count == 0)
            return Result<CheckoutPedidoResponse>.Fail("Carrinho vazio.");

        // ── Validar estoque ────────────────────────────────
        // Carrega todos os produtos referenciados no carrinho em uma única query
        // (batch) e valida se há estoque suficiente para cada item.
        var produtoIds = itensCarrinho.Select(i => i.ProdutoId).Distinct().ToList();
        var produtosDict = (await produtos.GetByIdsAsync(produtoIds, ct))
            .ToDictionary(p => p.Id);

        foreach (var item in itensCarrinho)
        {
            if (!produtosDict.TryGetValue(item.ProdutoId, out var produto))
                return Result<CheckoutPedidoResponse>.Fail($"Produto '{item.Nome}' não encontrado.");

            if (produto.Estoque < item.Quantidade)
                return Result<CheckoutPedidoResponse>.Fail(
                    $"Estoque insuficiente para '{produto.Nome}'. Disponível: {produto.Estoque}, solicitado: {item.Quantidade}.");

            // Combo: valida estoque dos componentes
            if (produto.ComboItens.Count > 0)
            {
                foreach (var comp in produto.ComboItens)
                {
                    if (!produtosDict.TryGetValue(comp.ProdutoId, out var compProduto))
                        return Result<CheckoutPedidoResponse>.Fail($"Componente do combo não encontrado (ProdutoId={comp.ProdutoId}).");

                    var necessario = comp.Quantidade * item.Quantidade;
                    if (compProduto.Estoque < necessario)
                        return Result<CheckoutPedidoResponse>.Fail(
                            $"Estoque insuficiente para o componente '{compProduto.Nome}' do combo '{produto.Nome}'. " +
                            $"Disponível: {compProduto.Estoque}, necessário: {necessario}.");
                }
            }
        }

        // ── Decrementar estoque e criar movimentos ────────
        var movimentosEstoque = new List<MovimentoEstoque>();
        foreach (var item in itensCarrinho)
        {
            var produto = produtosDict[item.ProdutoId];
            produto.Estoque -= item.Quantidade;
            movimentosEstoque.Add(new MovimentoEstoque
            {
                Id = Guid.NewGuid(),
                ProdutoId = produto.Id,
                Tipo = TipoMovimentoEstoque.Venda,
                Quantidade = -item.Quantidade,
                SaldoResultante = produto.Estoque,
                Data = clock.UtcNow
                // PedidoId será setado após criar o pedido
            });

            // Decrementar componentes do combo também
            foreach (var comp in produto.ComboItens)
            {
                var compProduto = produtosDict[comp.ProdutoId];
                var qtdComponente = comp.Quantidade * item.Quantidade;
                compProduto.Estoque -= qtdComponente;
                movimentosEstoque.Add(new MovimentoEstoque
                {
                    Id = Guid.NewGuid(),
                    ProdutoId = compProduto.Id,
                    Tipo = TipoMovimentoEstoque.Venda,
                    Quantidade = -qtdComponente,
                    SaldoResultante = compProduto.Estoque,
                    Data = clock.UtcNow
                });
            }
        }

        // ── Criar pedido ──────────────────────────────────
        var pedido = new Pedido
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            ClienteNome = clienteNome,
            Total = itensCarrinho.Sum(i => i.Preco * i.Quantidade),
            Data = clock.UtcNow,
            StatusAtual = PedidoEtapa.Criado
        };

        foreach (var item in itensCarrinho)
            pedido.Itens.Add(new PedidoItem
            {
                Id = Guid.NewGuid(),
                PedidoId = pedido.Id,
                ProdutoId = item.ProdutoId,
                Nome = item.Nome,
                Preco = item.Preco,
                FotoUrl = item.FotoUrl,
                Quantidade = item.Quantidade
            });

        pedido.Timeline.Add(new PedidoTimelineEvento
        {
            Id = Guid.NewGuid(),
            PedidoId = pedido.Id,
            Etapa = PedidoEtapa.Criado,
            Em = clock.UtcNow,
            Por = PedidoActor.Cliente
        });

        var linhasVenda = itensCarrinho.Select(i => new Venda
        {
            Id = Guid.NewGuid(),
            PedidoId = pedido.Id,
            ProdutoId = i.ProdutoId,
            ProdutoNome = i.Nome,
            Quantidade = i.Quantidade,
            Total = i.Preco * i.Quantidade,
            Data = clock.UtcNow
        });

        // Vincular PedidoId nos movimentos
        foreach (var m in movimentosEstoque)
            m.PedidoId = pedido.Id;

        await pedidos.AddAsync(pedido, ct);
        await vendas.AddRangeAsync(linhasVenda, ct);
        await movimentos.AddRangeAsync(movimentosEstoque, ct);
        await carrinho.ClearAsync(usuarioId, ct);
        await pedidos.SaveChangesAsync(ct);

        logger.LogInformation("Checkout concluído. PedidoId={PedidoId} UsuarioId={UsuarioId} Movimentos={Movimentos}",
            pedido.Id, usuarioId, movimentosEstoque.Count);
        return Result<CheckoutPedidoResponse>.Ok(new CheckoutPedidoResponse(pedido.Id));
    }
}
