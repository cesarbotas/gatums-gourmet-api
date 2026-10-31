using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Auth.Logout;

public sealed class LogoutHandler(
    IRefreshTokenStore store,
    IRefreshTokenGenerator gen,
    IClock clock) : ILogoutHandler
{
    public async Task<Result<bool>> Handle(Guid usuarioId, LogoutRequest request, CancellationToken ct = default)
    {
        var hash = gen.Hash(request.RefreshToken);
        var token = await store.GetByHashAsync(hash, ct);

        // Idempotente: só revoga se o token existe, pertence ao usuário e ainda está ativo.
        // Nunca revela se o token era válido.
        if (token is not null && token.UsuarioId == usuarioId && !token.Revogado)
            await store.RevokeFamilyAsync(token.FamiliaId, "Logout", clock.UtcNow, ct);

        return Result<bool>.Ok(true);
    }
}
