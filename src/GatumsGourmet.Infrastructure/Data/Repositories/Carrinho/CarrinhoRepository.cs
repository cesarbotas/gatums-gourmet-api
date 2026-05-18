using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Entities.Carrinho;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data.Repositories.Carrinho;

public class CarrinhoRepository(GatumsDbContext db) : ICarrinhoRepository
{
    public Task<IReadOnlyList<CarrinhoItem>> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        db.CarrinhoItens.AsNoTracking().Where(i => i.UsuarioId == usuarioId).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<CarrinhoItem>)t.Result, ct);

    public Task<CarrinhoItem?> GetItemAsync(Guid usuarioId, Guid produtoId, CancellationToken ct = default) =>
        db.CarrinhoItens.FirstOrDefaultAsync(i => i.UsuarioId == usuarioId && i.ProdutoId == produtoId, ct);

    public async Task AddAsync(CarrinhoItem item, CancellationToken ct = default) =>
        await db.CarrinhoItens.AddAsync(item, ct);

    public Task RemoveAsync(CarrinhoItem item, CancellationToken ct = default)
    {
        db.CarrinhoItens.Remove(item);
        return Task.CompletedTask;
    }

    public async Task ClearAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var itens = await db.CarrinhoItens.Where(i => i.UsuarioId == usuarioId).ToListAsync(ct);
        db.CarrinhoItens.RemoveRange(itens);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
