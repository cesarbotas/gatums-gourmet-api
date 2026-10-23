using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Pedidos.CheckoutPedido;

public record CheckoutPedidoResponse(Guid PedidoId);

public interface ICheckoutPedidoHandler
{
    Task<Result<CheckoutPedidoResponse>> Handle(Guid usuarioId, string clienteNome, CancellationToken ct = default);
}
