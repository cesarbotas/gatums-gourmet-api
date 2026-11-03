using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Auth.LogoutAll;

public sealed class LogoutAllHandler(
    IRefreshTokenStore store,
    IClock clock) : ILogoutAllHandler
{
    public async Task<Result<bool>> Handle(Guid usuarioId, CancellationToken ct = default)
    {
        await store.RevokeAllForUserAsync(usuarioId, "LogoutAll", clock.UtcNow, ct);
        return Result<bool>.Ok(true);
    }
}
