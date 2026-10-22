using GatumsGourmet.Domain.Entities.Produtos;

namespace GatumsGourmet.Domain.Repositories;

public interface ICategoriaRepository
{
    Task<(IReadOnlyList<Categoria> Items, int Total)> ListAsync(int page, int pageSize, bool? apenasAtivas, CancellationToken ct = default);
    Task<Categoria?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsByNomeAsync(string nome, long? ignorarId = null, CancellationToken ct = default);
    Task<bool> TemProdutosVinculadosAsync(long categoriaId, CancellationToken ct = default);
    Task AddAsync(Categoria categoria, CancellationToken ct = default);
    Task DeleteAsync(Categoria categoria, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
