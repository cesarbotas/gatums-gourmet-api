using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Enums.Pagamentos;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ValidarPagamentoParaAprovacao;

public sealed class ValidarPagamentoParaAprovacaoHandler(ITransacaoRepository transacoes) : IValidarPagamentoParaAprovacaoHandler
{
    public async Task<Result<ValidarPagamentoParaAprovacaoResponse>> Handle(Guid transacaoId, CancellationToken ct = default)
    {
        var transacao = await transacoes.GetByIdAsync(transacaoId, ct);

        if (transacao is null)
        {
            return Result<ValidarPagamentoParaAprovacaoResponse>.Fail("Transação não encontrada.");
        }

        if (transacao.Status != StatusTransacao.Aprovado)
        {
            return Result<ValidarPagamentoParaAprovacaoResponse>.Fail("Transação não está aprovada.");
        }

        return Result<ValidarPagamentoParaAprovacaoResponse>.Ok(ValidarPagamentoParaAprovacaoMapper.Map(transacao));
    }
}

