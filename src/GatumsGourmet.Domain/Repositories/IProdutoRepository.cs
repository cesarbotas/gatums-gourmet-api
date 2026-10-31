using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Enums.Produtos;

namespace GatumsGourmet.Domain.Repositories;

public interface IProdutoRepository
{
    Task<(IReadOnlyList<Produto> Items, int Total)> ListAsync(int page, int pageSize, string? search, long? categoriaId, TipoProduto? tipo, bool? disponivel, CancellationToken ct = default);
    Task<Produto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retorna as <c>FotoUrl</c> atualmente referenciadas por produtos (fonte de
    /// verdade para a limpeza de imagens órfãs). Snapshots em pedidos/carrinho
    /// não contam — a imagem só precisa viver enquanto um produto apontar p/ ela.
    /// </summary>
    Task<IReadOnlyList<string>> ListFotoUrlsEmUsoAsync(CancellationToken ct = default);

    Task AddAsync(Produto produto, CancellationToken ct = default);
    Task DeleteAsync(Produto produto, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
