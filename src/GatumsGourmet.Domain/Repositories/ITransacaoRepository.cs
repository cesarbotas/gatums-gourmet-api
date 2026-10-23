using GatumsGourmet.Domain.Entities.Pagamentos;

namespace GatumsGourmet.Domain.Repositories;

public interface ITransacaoRepository
{
    Task<TransacaoPagamento?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TransacaoPagamento?> GetByPedidoIdAsync(Guid pedidoId, CancellationToken ct = default);
    Task AddAsync(TransacaoPagamento transacao, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
