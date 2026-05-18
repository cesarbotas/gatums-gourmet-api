using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Entities.Pedidos;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data.Repositories.Pedidos;

public class PedidoRepository(GatumsDbContext db) : IPedidoRepository
{
    public async Task<(IReadOnlyList<Pedido> Items, int Total)> ListAsync(Guid? usuarioId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.Pedidos.Include(p => p.Itens).Include(p => p.Timeline).Include(p => p.Pagamento).AsNoTracking().AsQueryable();
        if (usuarioId.HasValue) q = q.Where(p => p.UsuarioId == usuarioId.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.Data).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<Pedido?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Pedidos.Include(p => p.Itens).Include(p => p.Timeline).Include(p => p.Pagamento).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Pedido pedido, CancellationToken ct = default) =>
        await db.Pedidos.AddAsync(pedido, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
