using GatumsGourmet.Domain.Entities;
using GatumsGourmet.Domain.Enums;

namespace GatumsGourmet.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Usuario usuario, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IProdutoRepository
{
    Task<(IReadOnlyList<Produto> Items, int Total)> ListAsync(int page, int pageSize, string? search, string? categoria, TipoProduto? tipo, bool? disponivel, CancellationToken ct = default);
    Task<Produto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListCategoriasAsync(CancellationToken ct = default);
    Task AddAsync(Produto produto, CancellationToken ct = default);
    Task DeleteAsync(Produto produto, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IClienteRepository
{
    Task<(IReadOnlyList<Cliente> Items, int Total)> ListAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Cliente cliente, CancellationToken ct = default);
    Task DeleteAsync(Cliente cliente, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface ICarrinhoRepository
{
    Task<IReadOnlyList<CarrinhoItem>> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    Task<CarrinhoItem?> GetItemAsync(Guid usuarioId, Guid produtoId, CancellationToken ct = default);
    Task AddAsync(CarrinhoItem item, CancellationToken ct = default);
    Task RemoveAsync(CarrinhoItem item, CancellationToken ct = default);
    Task ClearAsync(Guid usuarioId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IPedidoRepository
{
    Task<(IReadOnlyList<Pedido> Items, int Total)> ListAsync(Guid? usuarioId, int page, int pageSize, CancellationToken ct = default);
    Task<Pedido?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Pedido pedido, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface ITransacaoRepository
{
    Task<TransacaoPagamento?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TransacaoPagamento?> GetByPedidoIdAsync(Guid pedidoId, CancellationToken ct = default);
    Task AddAsync(TransacaoPagamento transacao, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IVendaRepository
{
    Task AddRangeAsync(IEnumerable<Venda> vendas, CancellationToken ct = default);
    Task<IReadOnlyList<Venda>> GetUltimos30DiasAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
