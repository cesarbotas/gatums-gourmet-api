using GatumsGourmet.Domain.Entities.Pagamentos;

namespace GatumsGourmet.Domain.Services.Interfaces;

public interface IPixOrdersProvider
{
    Task<PixOrderResult> CriarOrderAsync(PixOrderRequest request, CancellationToken ct = default);

    Task<PixOrderStatus?> ConsultarOrderAsync(string orderId, CancellationToken ct = default);
}