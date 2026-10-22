using GatumsGourmet.Domain.Entities.Vendas;

namespace GatumsGourmet.Domain.Repositories;

public interface IVendaRepository
{
    Task AddRangeAsync(IEnumerable<Venda> vendas, CancellationToken ct = default);
    Task<IReadOnlyList<Venda>> GetUltimos30DiasAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
