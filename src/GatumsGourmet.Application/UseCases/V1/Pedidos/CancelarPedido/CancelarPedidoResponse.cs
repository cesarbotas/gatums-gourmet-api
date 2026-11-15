namespace GatumsGourmet.Application.UseCases.V1.Pedidos.CancelarPedido;

public record CancelarPedidoResponse(
    Guid PedidoId,
    string StatusAnterior,
    int ItensEstornados);
