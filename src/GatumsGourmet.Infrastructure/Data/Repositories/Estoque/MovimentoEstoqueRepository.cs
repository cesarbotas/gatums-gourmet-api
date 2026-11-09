using GatumsGourmet.Domain.Entities.Estoque;
using GatumsGourmet.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data.Repositories.Estoque;

public class MovimentoEstoqueRepository(GatumsDbContext db) : IMovimentoEstoqueRepository
{
    public async Task<IReadOnlyList<MovimentoEstoque>> ListByProdutoAsync(Guid produtoId, CancellationToken ct = default) =>
        await db.MovimentosEstoque.Where(m => m.ProdutoId == produtoId).OrderByDescending(m => m.Data).ToListAsync(ct);

    public async Task AddAsync(MovimentoEstoque movimento, CancellationToken ct = default) =>
        await db.MovimentosEstoque.AddAsync(movimento, ct);

    public async Task AddRangeAsync(IEnumerable<MovimentoEstoque> movimentos, CancellationToken ct = default) =>
        await db.MovimentosEstoque.AddRangeAsync(movimentos, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
