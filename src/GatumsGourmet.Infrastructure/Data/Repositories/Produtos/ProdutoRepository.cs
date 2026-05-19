using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Enums.Produtos;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data.Repositories.Produtos;

public class ProdutoRepository(GatumsDbContext db) : IProdutoRepository
{
    public async Task<(IReadOnlyList<Produto> Items, int Total)> ListAsync(int page, int pageSize, string? search, string? categoria, TipoProduto? tipo, bool? disponivel, CancellationToken ct = default)
    {
        var q = db.Produtos.Include(p => p.ComboItens).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(p => p.Nome.Contains(search) || p.Descricao.Contains(search));
        if (!string.IsNullOrWhiteSpace(categoria)) q = q.Where(p => p.Categoria == categoria);
        if (tipo.HasValue) q = q.Where(p => p.Tipo == tipo.Value);
        if (disponivel.HasValue) q = q.Where(p => p.Disponivel == disponivel.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(p => p.Nome).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<Produto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Produtos.Include(p => p.ComboItens).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<IReadOnlyList<string>> ListCategoriasAsync(CancellationToken ct = default) =>
        db.Produtos.AsNoTracking().Select(p => p.Categoria).Distinct().OrderBy(c => c).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<string>)t.Result, ct);

    public async Task AddAsync(Produto produto, CancellationToken ct = default) =>
        await db.Produtos.AddAsync(produto, ct);

    public Task DeleteAsync(Produto produto, CancellationToken ct = default)
    {
        db.Produtos.Remove(produto);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
