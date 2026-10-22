using GatumsGourmet.Domain.Entities.Pedidos;

namespace GatumsGourmet.Domain.Repositories;

public interface IPedidoRepository
{
    Task<(IReadOnlyList<Pedido> Items, int Total)> ListAsync(Guid? usuarioId, int page, int pageSize, CancellationToken ct = default);
    Task<Pedido?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Pedido pedido, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
