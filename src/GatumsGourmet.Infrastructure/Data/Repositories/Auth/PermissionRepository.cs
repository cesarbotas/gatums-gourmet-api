using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data.Repositories.Auth;

public class PermissionRepository(GatumsDbContext db) : IPermissionRepository
{
    public Task<Permission?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Permissions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Permission?> GetByCodigoAsync(string codigo, CancellationToken ct = default) =>
        db.Permissions.AsNoTracking().FirstOrDefaultAsync(p => p.Codigo == codigo, ct);

    public async Task<IReadOnlyList<Permission>> ListAsync(CancellationToken ct = default) =>
        await db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Codigo)
            .ToListAsync(ct);

    public async Task AddAsync(Permission permission, CancellationToken ct = default) =>
        await db.Permissions.AddAsync(permission, ct);

    public void Update(Permission permission) => db.Permissions.Update(permission);

    public void Remove(Permission permission) => db.Permissions.Remove(permission);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
