using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Entities.Clientes;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data.Repositories.Clientes;

public class ClienteRepository(GatumsDbContext db) : IClienteRepository
{
    public async Task<(IReadOnlyList<Cliente> Items, int Total)> ListAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var q = db.Clientes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(c => c.Nome.Contains(search) || c.Email.Contains(search));
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(c => c.Nome).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Cliente cliente, CancellationToken ct = default) =>
        await db.Clientes.AddAsync(cliente, ct);

    public Task DeleteAsync(Cliente cliente, CancellationToken ct = default)
    {
        db.Clientes.Remove(cliente);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
