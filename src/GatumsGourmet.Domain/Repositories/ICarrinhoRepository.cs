using GatumsGourmet.Domain.Entities.Carrinho;

namespace GatumsGourmet.Domain.Repositories;

public interface ICarrinhoRepository
{
    Task<IReadOnlyList<CarrinhoItem>> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    Task<CarrinhoItem?> GetItemAsync(Guid usuarioId, Guid produtoId, CancellationToken ct = default);
    Task AddAsync(CarrinhoItem item, CancellationToken ct = default);
    Task RemoveAsync(CarrinhoItem item, CancellationToken ct = default);
    Task ClearAsync(Guid usuarioId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
