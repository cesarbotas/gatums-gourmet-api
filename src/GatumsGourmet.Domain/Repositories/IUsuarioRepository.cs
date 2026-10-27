using GatumsGourmet.Domain.Entities.Auth;

namespace GatumsGourmet.Domain.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Usuario usuario, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Usuario>> ListAsync(CancellationToken ct = default);
    Task<Usuario?> GetByIdComRolesAsync(Guid id, CancellationToken ct = default);
    Task SetRolesAsync(Guid usuarioId, IEnumerable<int> roleIds, CancellationToken ct = default);
    Task SetAtivoAsync(Guid usuarioId, bool ativo, CancellationToken ct = default);
    Task<int> CountAdminsAtivosAsync(CancellationToken ct = default);
}
