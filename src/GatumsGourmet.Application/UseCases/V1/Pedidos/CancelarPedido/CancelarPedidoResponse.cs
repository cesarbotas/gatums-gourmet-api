using GatumsGourmet.Domain.Enums.Pedidos;

namespace GatumsGourmet.Application.UseCases.V1.Pedidos.CancelarPedido;

public record CancelarPedidoResponse(
    Guid PedidoId,
    PedidoEtapa StatusAnterior,
    int ItensEstornados);
