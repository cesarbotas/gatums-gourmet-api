using GatumsGourmet.Domain.Entities.Auth;

namespace GatumsGourmet.Domain.Repositories;

public interface IRefreshTokenStore
{
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task RevokeFamilyAsync(Guid familiaId, string motivo, DateTime em, CancellationToken ct = default);
    Task RevokeAllForUserAsync(Guid usuarioId, string motivo, DateTime em, CancellationToken ct = default);
    Task<int> DeleteExpiredAsync(DateTime cutoff, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
