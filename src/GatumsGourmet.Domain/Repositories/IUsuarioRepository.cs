using GatumsGourmet.Domain.Entities.Auth;

namespace GatumsGourmet.Domain.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Usuario usuario, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
