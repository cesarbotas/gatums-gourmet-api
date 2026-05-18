using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Entities;
using GatumsGourmet.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data.Repositories;

public class UsuarioRepository(GatumsDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(Usuario usuario, CancellationToken ct = default) =>
        await db.Usuarios.AddAsync(usuario, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

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

public class TransacaoRepository(GatumsDbContext db) : ITransacaoRepository
{
    public Task<TransacaoPagamento?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.TransacoesPagamento.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<TransacaoPagamento?> GetByPedidoIdAsync(Guid pedidoId, CancellationToken ct = default) =>
        db.TransacoesPagamento.AsNoTracking().FirstOrDefaultAsync(t => t.PedidoId == pedidoId, ct);

    public async Task AddAsync(TransacaoPagamento transacao, CancellationToken ct = default) =>
        await db.TransacoesPagamento.AddAsync(transacao, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

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
