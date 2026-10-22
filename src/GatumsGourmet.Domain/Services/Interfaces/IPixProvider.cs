using GatumsGourmet.Domain.Entities.Pagamentos;

namespace GatumsGourmet.Domain.Services.Interfaces;

public interface IPixProvider
{
    Task<PixResult> GerarAsync(Guid transacaoId, decimal valor, CancellationToken ct = default);
    Task<bool> ConfirmarAsync(Guid transacaoId, CancellationToken ct = default);
}
