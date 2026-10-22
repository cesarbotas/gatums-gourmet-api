using GatumsGourmet.Domain.Entities.Clientes;

namespace GatumsGourmet.Domain.Repositories;

public interface IClienteRepository
{
    Task<(IReadOnlyList<Cliente> Items, int Total)> ListAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Cliente cliente, CancellationToken ct = default);
    Task DeleteAsync(Cliente cliente, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
