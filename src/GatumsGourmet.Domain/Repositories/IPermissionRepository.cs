using GatumsGourmet.Domain.Entities.Auth;

namespace GatumsGourmet.Domain.Repositories;

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Permission?> GetByCodigoAsync(string codigo, CancellationToken ct = default);
    Task<IReadOnlyList<Permission>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Permission permission, CancellationToken ct = default);
    void Update(Permission permission);
    void Remove(Permission permission);
    Task SaveChangesAsync(CancellationToken ct = default);
}
