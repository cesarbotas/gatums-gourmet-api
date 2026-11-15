using GatumsGourmet.Domain.Entities.Estoque;

namespace GatumsGourmet.Domain.Repositories;

public interface IMovimentoEstoqueRepository
{
    Task<IReadOnlyList<MovimentoEstoque>> ListByProdutoAsync(Guid produtoId, CancellationToken ct = default);
    Task AddAsync(MovimentoEstoque movimento, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<MovimentoEstoque> movimentos, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
