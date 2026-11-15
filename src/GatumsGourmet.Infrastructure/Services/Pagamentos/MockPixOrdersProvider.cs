using System.Collections.Concurrent;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Infrastructure.Services.Pagamentos;

/// <summary>
/// Mock da Orders API para desenvolvimento/testes (sem AccessToken). Toda order criada é consultável
/// como processed/accredited.
/// </summary>
public sealed class MockPixOrdersProvider(IClock clock) : IPixOrdersProvider
{
    private static readonly ConcurrentDictionary<string, byte> Orders = new();

    public Task<PixOrderResult> CriarOrderAsync(PixOrderRequest request, CancellationToken ct = default)
    {
        var orderId = $"ORD-MOCK-{Guid.NewGuid():N}";
        Orders[orderId] = 0;

        var copiaCola = $"00020126580014BR.GOV.BCB.PIX0136{request.TransacaoId}5204000053039865802BR5913GatumsGourmet6009SAO PAULO62070503***6304ABCD";
        return Task.FromResult(new PixOrderResult(
            OrderId: orderId,
            CopiaCola: copiaCola,
            QrCodeBase64: null,
            TicketUrl: null,
            ExpiraEm: clock.UtcNow.AddMinutes(30)));
    }

    public Task<PixOrderStatus?> ConsultarOrderAsync(string orderId, CancellationToken ct = default) =>
        Task.FromResult<PixOrderStatus?>(
            Orders.ContainsKey(orderId) ? new PixOrderStatus("processed", "accredited") : null);
}