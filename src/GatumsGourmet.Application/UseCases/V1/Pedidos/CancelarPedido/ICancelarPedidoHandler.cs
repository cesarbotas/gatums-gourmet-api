using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Enums.Pedidos;

namespace GatumsGourmet.Application.UseCases.V1.Pedidos.CancelarPedido;

public interface ICancelarPedidoHandler
{
    /// <param name="pedidoId">ID do pedido a cancelar.</param>
    /// <param name="request">Motivo opcional.</param>
    /// <param name="requestingUserId">UserId extraído do JWT (para verificação de propriedade).</param>
    /// <param name="actor">Admin pode cancelar mais etapas que Cliente.</param>
    Task<Result<CancelarPedidoResponse>> Handle(
        Guid pedidoId,
        CancelarPedidoRequest request,
        Guid requestingUserId,
        PedidoActor actor,
        CancellationToken ct = default);
}
