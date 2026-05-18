using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Entities;
using GatumsGourmet.Domain.Enums;
using GatumsGourmet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.Pedidos.CheckoutPedido;

public record CheckoutPedidoResponse(Guid PedidoId);

public interface ICheckoutPedidoHandler
{
    Task<Result<CheckoutPedidoResponse>> Handle(Guid usuarioId, string clienteNome, CancellationToken ct = default);
}

public sealed class CheckoutPedidoHandler(
    ICarrinhoRepository carrinho,
    IPedidoRepository pedidos,
    IVendaRepository vendas,
    IClock clock,
    ILogger<CheckoutPedidoHandler> logger) : ICheckoutPedidoHandler
{
    public async Task<Result<CheckoutPedidoResponse>> Handle(Guid usuarioId, string clienteNome, CancellationToken ct = default)
    {
        var itensCarrinho = await carrinho.GetByUsuarioAsync(usuarioId, ct);
        if (itensCarrinho.Count == 0)
            return Result<CheckoutPedidoResponse>.Fail("Carrinho vazio.");

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
            pedido.Itens.Add(new PedidoItem { Id = Guid.NewGuid(), PedidoId = pedido.Id, ProdutoId = item.ProdutoId, Nome = item.Nome, Preco = item.Preco, FotoUrl = item.FotoUrl, Quantidade = item.Quantidade });

        pedido.Timeline.Add(new PedidoTimelineEvento { Id = Guid.NewGuid(), PedidoId = pedido.Id, Etapa = PedidoEtapa.Criado, Em = clock.UtcNow, Por = PedidoActor.Cliente });

        var linhasVenda = itensCarrinho.Select(i => new Venda { Id = Guid.NewGuid(), PedidoId = pedido.Id, ProdutoId = i.ProdutoId, ProdutoNome = i.Nome, Quantidade = i.Quantidade, Total = i.Preco * i.Quantidade, Data = clock.UtcNow });

        await pedidos.AddAsync(pedido, ct);
        await vendas.AddRangeAsync(linhasVenda, ct);
        await carrinho.ClearAsync(usuarioId, ct);
        await pedidos.SaveChangesAsync(ct);

        logger.LogInformation("Checkout concluído. PedidoId={PedidoId} UsuarioId={UsuarioId}", pedido.Id, usuarioId);
        return Result<CheckoutPedidoResponse>.Ok(new CheckoutPedidoResponse(pedido.Id));
    }
}
