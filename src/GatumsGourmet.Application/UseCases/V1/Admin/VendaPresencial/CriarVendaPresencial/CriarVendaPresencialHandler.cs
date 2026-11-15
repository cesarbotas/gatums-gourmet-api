using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Estoque;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Entities.Vendas;
using GatumsGourmet.Domain.Enums.Estoque;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.CriarVendaPresencial;

/// <summary>
/// Cria um pedido presencial: carrega produtos, valida disponibilidade e estoque,
/// calcula o total no servidor (nunca aceita preço do cliente), cria o pedido
/// diretamente na etapa <see cref="PedidoEtapa.Pagamento"/>, decrementa estoque
/// e registra movimentos — tudo em uma única chamada atômica.
/// </summary>
public sealed class CriarVendaPresencialHandler(
    IProdutoRepository produtos,
    IPedidoRepository pedidos,
    IVendaRepository vendas,
    IMovimentoEstoqueRepository movimentos,
    IClock clock,
    ILogger<CriarVendaPresencialHandler> logger) : ICriarVendaPresencialHandler
{
    public async Task<Result<CriarVendaPresencialResponse>> Handle(
        CriarVendaPresencialRequest request,
        CancellationToken ct = default)
    {
        if (request.Itens is null || request.Itens.Count == 0)
            return Result<CriarVendaPresencialResponse>.Fail("Nenhum item informado.");

        // ── 1. Carregar produtos em batch ──────────────────────
        var produtoIds = request.Itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtosDict = (await produtos.GetByIdsAsync(produtoIds, ct)).ToDictionary(p => p.Id);

        // ── 2. Validar disponibilidade e estoque ───────────────
        foreach (var item in request.Itens)
        {
            if (!produtosDict.TryGetValue(item.ProdutoId, out var produto))
                return Result<CriarVendaPresencialResponse>.Fail(
                    $"Produto '{item.ProdutoId}' não encontrado.");

            if (!produto.Disponivel)
                return Result<CriarVendaPresencialResponse>.Fail(
                    $"Produto '{produto.Nome}' está indisponível.");

            if (produto.Estoque < item.Quantidade)
                return Result<CriarVendaPresencialResponse>.Fail(
                    $"Estoque insuficiente para '{produto.Nome}'. Disponível: {produto.Estoque}, solicitado: {item.Quantidade}.");
        }

        // ── 3. Montar pedido (total calculado pelo servidor) ───
        var clienteNome = string.IsNullOrWhiteSpace(request.ClienteNome) ? "Balcão" : request.ClienteNome.Trim();
        var pedidoId = Guid.NewGuid();
        var agora = clock.UtcNow;

        var itensPedido = request.Itens.Select(item =>
        {
            var p = produtosDict[item.ProdutoId];
            return new PedidoItem
            {
                Id = Guid.NewGuid(),
                PedidoId = pedidoId,
                ProdutoId = p.Id,
                Nome = p.Nome,
                Preco = p.Preco,
                FotoUrl = p.FotoUrl,
                Quantidade = item.Quantidade
            };
        }).ToList();

        var total = itensPedido.Sum(i => i.Preco * i.Quantidade);

        var pedido = new Pedido
        {
            Id = pedidoId,
            UsuarioId = request.OperadorId,
            ClienteNome = clienteNome,
            Total = total,
            Data = agora,
            // Venda presencial entra direto em Pagamento (bypassa Criado/Aprovado do fluxo online)
            StatusAtual = PedidoEtapa.Pagamento,
            Canal = CanalVenda.Presencial,
            Itens = itensPedido
        };

        pedido.Timeline.Add(new PedidoTimelineEvento
        {
            Id = Guid.NewGuid(),
            PedidoId = pedidoId,
            Etapa = PedidoEtapa.Pagamento,
            Em = agora,
            Por = PedidoActor.Admin,
            Observacao = $"Venda presencial — operador {request.OperadorId}"
        });

        // ── 4. Decrementar estoque + movimentos (atômico) ──────
        var movimentosEstoque = new List<MovimentoEstoque>();
        foreach (var item in request.Itens)
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
                PedidoId = pedidoId,
                UsuarioId = request.OperadorId,
                Observacao = $"Venda presencial — operador {request.OperadorId}",
                Data = agora
            });
        }

        var linhasVenda = request.Itens.Select(item =>
        {
            var p = produtosDict[item.ProdutoId];
            return new Venda
            {
                Id = Guid.NewGuid(),
                PedidoId = pedidoId,
                ProdutoId = p.Id,
                ProdutoNome = p.Nome,
                Quantidade = item.Quantidade,
                Total = p.Preco * item.Quantidade,
                Data = agora
            };
        });

        await pedidos.AddAsync(pedido, ct);
        await vendas.AddRangeAsync(linhasVenda, ct);
        await movimentos.AddRangeAsync(movimentosEstoque, ct);
        await pedidos.SaveChangesAsync(ct);

        logger.LogInformation(
            "Venda presencial criada. PedidoId={PedidoId} Operador={Operador} Total={Total} Itens={Itens}",
            pedidoId, request.OperadorId, total, request.Itens.Count);

        var itensResp = itensPedido.Select(i => new CriarVendaPresencialItemResponse(
            i.ProdutoId, i.Nome, i.Quantidade, i.Preco, i.Preco * i.Quantidade)).ToList();

        return Result<CriarVendaPresencialResponse>.Ok(new CriarVendaPresencialResponse(
            pedidoId, total, itensResp, "presencial", request.OperadorId, clienteNome, agora));
    }
}
