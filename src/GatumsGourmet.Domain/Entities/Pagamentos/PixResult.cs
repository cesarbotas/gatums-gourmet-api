namespace GatumsGourmet.Domain.Entities.Pagamentos;

public record PixResult(
    string CopiaCola,
    DateTime ExpiraEm,
    string? QrCodeBase64 = null,
    string? OperadoraRef = null);
