namespace GatumsGourmet.Domain.Services.Interfaces;

/// <summary>
/// Valida o header <c>x-signature</c> dos webhooks de orders do Mercado Pago.
/// </summary>
public interface IMercadoPagoWebhookSignatureValidator
{
    bool IsValid(string? signatureHeader, string? requestId, string? dataId);
}
