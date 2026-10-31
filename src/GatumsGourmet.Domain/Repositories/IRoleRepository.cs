using GatumsGourmet.Domain.Entities.Auth;

namespace GatumsGourmet.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Role?> GetByNomeAsync(string nome, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Role role, CancellationToken ct = default);
    void Update(Role role);
    void Remove(Role role);
    Task SetPermissionsAsync(int roleId, IEnumerable<int> permissionIds, CancellationToken ct = default);
    Task<int> CountUsuariosAsync(int roleId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
