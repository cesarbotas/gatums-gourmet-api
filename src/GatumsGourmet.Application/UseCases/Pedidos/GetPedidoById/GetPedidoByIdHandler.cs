using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Pedidos.ListPedidos;

namespace GatumsGourmet.Application.UseCases.Pedidos.GetPedidoById;

public interface IGetPedidoByIdHandler
{
    Task<Result<PedidoResponse>> Handle(Guid id, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);
}

public sealed class GetPedidoByIdHandler(IPedidoRepository pedidos) : IGetPedidoByIdHandler
{
    public async Task<Result<PedidoResponse>> Handle(Guid id, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var pedido = await pedidos.GetByIdAsync(id, ct);
        if (pedido is null) return Result<PedidoResponse>.Fail("Pedido não encontrado.");
        if (!isAdmin && pedido.UsuarioId != requestingUserId) return Result<PedidoResponse>.Fail("Acesso negado.");
        return Result<PedidoResponse>.Ok(ListPedidosHandler.MapPedido(pedido));
    }
}
