using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.Pagamentos.ConfirmarPagamentoPix;

public interface IConfirmarPagamentoPixHandler
{
    Task<Result<TransacaoResponse>> Handle(Guid transacaoId, CancellationToken ct = default);
}

public sealed class ConfirmarPagamentoPixHandler(
    ITransacaoRepository transacoes,
    IPixProvider pixProvider,
    IClock clock,
    ILogger<ConfirmarPagamentoPixHandler> logger) : IConfirmarPagamentoPixHandler
{
    public async Task<Result<TransacaoResponse>> Handle(Guid transacaoId, CancellationToken ct = default)
    {
        var transacao = await transacoes.GetByIdAsync(transacaoId, ct);
        if (transacao is null) return Result<TransacaoResponse>.Fail("Transação não encontrada.");
        if (transacao.Metodo != MetodoPagamento.Pix) return Result<TransacaoResponse>.Fail("Transação não é Pix.");
        if (transacao.Status != StatusTransacao.Pendente) return Result<TransacaoResponse>.Fail("Transação já processada.");

        var confirmado = await pixProvider.ConfirmarAsync(transacaoId, ct);
        transacao.Status = confirmado ? StatusTransacao.Aprovado : StatusTransacao.Pendente;
        transacao.AtualizadoEm = clock.UtcNow;

        await transacoes.SaveChangesAsync(ct);
        logger.LogInformation("Pix confirmado. TransacaoId={Id} Confirmado={Confirmado}", transacaoId, confirmado);
        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }
}
