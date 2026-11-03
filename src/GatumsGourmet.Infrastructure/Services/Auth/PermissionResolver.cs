using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Services.Auth;

public class PermissionResolver(GatumsDbContext db) : IPermissionResolver
{
    public async Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissoes)> ResolveAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var roles = await db.UsuarioRoles
            .AsNoTracking()
            .Where(ur => ur.UsuarioId == usuarioId)
            .Select(ur => ur.Role.Nome)
            .Distinct()
            .ToListAsync(ct);

        var permissoes = await db.UsuarioRoles
            .AsNoTracking()
            .Where(ur => ur.UsuarioId == usuarioId)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Codigo))
            .Distinct()
            .ToListAsync(ct);

        return (roles, permissoes);
    }
}
