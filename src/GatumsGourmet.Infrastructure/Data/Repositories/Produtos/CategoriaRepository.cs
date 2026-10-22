using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Produtos;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data.Repositories.Produtos;

public class CategoriaRepository(GatumsDbContext db) : ICategoriaRepository
{
    public async Task<IReadOnlyList<Categoria>> ListAsync(bool? apenasAtivas, CancellationToken ct = default)
    {
        var q = db.Categorias.AsNoTracking().AsQueryable();
        if (apenasAtivas == true) q = q.Where(c => c.Ativo);
        return await q.OrderBy(c => c.Nome).ToListAsync(ct);
    }

    public Task<Categoria?> GetByIdAsync(long id, CancellationToken ct = default) =>
        db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistsByNomeAsync(string nome, long? ignorarId = null, CancellationToken ct = default) =>
        db.Categorias.AsNoTracking()
            .AnyAsync(c => c.Nome.ToLower() == nome.ToLower() && (ignorarId == null || c.Id != ignorarId), ct);

    public Task<bool> TemProdutosVinculadosAsync(long categoriaId, CancellationToken ct = default) =>
        db.Produtos.AsNoTracking().AnyAsync(p => p.CategoriaId == categoriaId, ct);

    public async Task AddAsync(Categoria categoria, CancellationToken ct = default) =>
        await db.Categorias.AddAsync(categoria, ct);

    public Task DeleteAsync(Categoria categoria, CancellationToken ct = default)
    {
        db.Categorias.Remove(categoria);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

