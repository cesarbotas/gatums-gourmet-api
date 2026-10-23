using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ConsultarStatusPagamento;

public interface IConsultarStatusPagamentoHandler
{
    Task<Result<TransacaoResponse>> Handle(Guid transacaoId, CancellationToken ct = default);
}

public sealed class ConsultarStatusPagamentoHandler(ITransacaoRepository transacoes) : IConsultarStatusPagamentoHandler
{
    public async Task<Result<TransacaoResponse>> Handle(Guid transacaoId, CancellationToken ct = default)
    {
        var transacao = await transacoes.GetByIdAsync(transacaoId, ct);
        if (transacao is null) return Result<TransacaoResponse>.Fail("Transação não encontrada.");
        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }
}

