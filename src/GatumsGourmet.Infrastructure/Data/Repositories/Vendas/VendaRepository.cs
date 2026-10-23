using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Vendas;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data.Repositories.Vendas;

public class VendaRepository(GatumsDbContext db) : IVendaRepository
{
    public async Task AddRangeAsync(IEnumerable<Venda> vendas, CancellationToken ct = default) =>
        await db.Vendas.AddRangeAsync(vendas, ct);

    public Task<IReadOnlyList<Venda>> GetUltimos30DiasAsync(CancellationToken ct = default)
    {
        var desde = DateTime.UtcNow.AddDays(-30);
        return db.Vendas.AsNoTracking().Where(v => v.Data >= desde).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Venda>)t.Result, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

