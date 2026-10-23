using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Pagamentos;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data.Repositories.Pagamentos;

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

