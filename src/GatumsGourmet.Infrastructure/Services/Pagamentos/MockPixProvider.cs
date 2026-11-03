using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Infrastructure.Services.Pagamentos;

// Mock implementation for Development â€” replace with real PIX provider in Production
public class MockPixProvider : IPixProvider
{
    public Task<PixResult> GerarAsync(Guid transacaoId, decimal valor, CancellationToken ct = default)
    {
        var copiaCola = $"00020126580014BR.GOV.BCB.PIX0136{transacaoId}5204000053039865802BR5913GatumsGourmet6009SAO PAULO62070503***6304ABCD";
        return Task.FromResult(new PixResult(copiaCola, DateTime.UtcNow.AddMinutes(30)));
    }

    public Task<bool> ConfirmarAsync(Guid transacaoId, CancellationToken ct = default) =>
        Task.FromResult(true);
}
