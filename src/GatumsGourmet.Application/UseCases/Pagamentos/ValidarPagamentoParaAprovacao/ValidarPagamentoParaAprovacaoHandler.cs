using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Enums.Pagamentos;

namespace GatumsGourmet.Application.UseCases.Pagamentos.ValidarPagamentoParaAprovacao;

public interface IValidarPagamentoParaAprovacaoHandler
{
    Task<Result<TransacaoResponse>> Handle(Guid transacaoId, CancellationToken ct = default);
}

public sealed class ValidarPagamentoParaAprovacaoHandler(ITransacaoRepository transacoes) : IValidarPagamentoParaAprovacaoHandler
{
    public async Task<Result<TransacaoResponse>> Handle(Guid transacaoId, CancellationToken ct = default)
    {
        var transacao = await transacoes.GetByIdAsync(transacaoId, ct);
        if (transacao is null) return Result<TransacaoResponse>.Fail("Transação não encontrada.");
        if (transacao.Status != StatusTransacao.Aprovado) return Result<TransacaoResponse>.Fail("Transação não está aprovada.");
        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }
}

