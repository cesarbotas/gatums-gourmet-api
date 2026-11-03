using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data.Repositories.Auth;

public class RefreshTokenStore(GatumsDbContext db) : IRefreshTokenStore
{
    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await db.RefreshTokens.AddAsync(token, ct);

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task RevokeFamilyAsync(Guid familiaId, string motivo, DateTime em, CancellationToken ct = default)
    {
        var tokens = await db.RefreshTokens
            .Where(t => t.FamiliaId == familiaId && !t.Revogado)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.Revogado = true;
            token.RevogadoEm = em;
            token.MotivoRevogacao = motivo;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task RevokeAllForUserAsync(Guid usuarioId, string motivo, DateTime em, CancellationToken ct = default)
    {
        var tokens = await db.RefreshTokens
            .Where(t => t.UsuarioId == usuarioId && !t.Revogado)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.Revogado = true;
            token.RevogadoEm = em;
            token.MotivoRevogacao = motivo;
        }

        await db.SaveChangesAsync(ct);
    }

    public Task<int> DeleteExpiredAsync(DateTime cutoff, CancellationToken ct = default) =>
        db.RefreshTokens
            .Where(t => t.ExpiraEm < cutoff || (t.Revogado && t.RevogadoEm < cutoff))
            .ExecuteDeleteAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
