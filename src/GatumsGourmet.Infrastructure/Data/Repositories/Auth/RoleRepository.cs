using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data.Repositories.Auth;

public class RoleRepository(GatumsDbContext db) : IRoleRepository
{
    public Task<Role?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Role?> GetByNomeAsync(string nome, CancellationToken ct = default) =>
        db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Nome == nome, ct);

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default) =>
        await db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Nome)
            .ToListAsync(ct);

    public async Task AddAsync(Role role, CancellationToken ct = default) =>
        await db.Roles.AddAsync(role, ct);

    public void Update(Role role) => db.Roles.Update(role);

    public void Remove(Role role) => db.Roles.Remove(role);

    public async Task SetPermissionsAsync(int roleId, IEnumerable<int> permissionIds, CancellationToken ct = default)
    {
        var atuais = await db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(ct);

        db.RolePermissions.RemoveRange(atuais);

        var novos = permissionIds
            .Select(pid => new RolePermission { RoleId = roleId, PermissionId = pid });

        await db.RolePermissions.AddRangeAsync(novos, ct);
    }

    public Task<int> CountUsuariosAsync(int roleId, CancellationToken ct = default) =>
        db.UsuarioRoles.CountAsync(ur => ur.RoleId == roleId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
