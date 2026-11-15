namespace GatumsGourmet.Domain.Entities.Pagamentos;

public record PixOrderResult(
    string OrderId,
    string CopiaCola,
    string? QrCodeBase64,
    string? TicketUrl,
    DateTime ExpiraEm);