namespace GatumsGourmet.Application.UseCases.V1.Pedidos.CancelarPedido;

/// <param name="Motivo">Texto livre explicando o motivo do cancelamento. Gravado na timeline.</param>
public record CancelarPedidoRequest(string? Motivo = null);
