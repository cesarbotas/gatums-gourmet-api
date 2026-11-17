using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarWebhookMercadoPago;

public interface IProcessarWebhookMercadoPagoHandler
{
    Task<Result<bool>> Handle(string? paymentIdParam, MercadoPagoWebhookBody? body, CancellationToken ct = default);
}