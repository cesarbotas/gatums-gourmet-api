using GatumsGourmet.Application.Common;

namespace GatumsGourmet.Application.UseCases.Pedidos.CheckoutPedido;

public record CheckoutPedidoResponse(Guid PedidoId);

public interface ICheckoutPedidoHandler
{
    Task<Result<CheckoutPedidoResponse>> Handle(Guid usuarioId, string clienteNome, CancellationToken ct = default);
}
