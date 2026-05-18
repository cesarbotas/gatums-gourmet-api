using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Enums;
using GatumsGourmet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.Pagamentos.ProcessarPagamentoCartao;

public record ProcessarPagamentoCartaoRequest(string Numero, string Nome, string Validade, string Cvv);

public interface IProcessarPagamentoCartaoHandler
{
    Task<Result<TransacaoResponse>> Handle(Guid transacaoId, ProcessarPagamentoCartaoRequest request, CancellationToken ct = default);
}

public sealed class ProcessarPagamentoCartaoHandler(
    ITransacaoRepository transacoes,
    IPaymentGateway gateway,
    IClock clock,
    ILogger<ProcessarPagamentoCartaoHandler> logger) : IProcessarPagamentoCartaoHandler
{
    public async Task<Result<TransacaoResponse>> Handle(Guid transacaoId, ProcessarPagamentoCartaoRequest request, CancellationToken ct = default)
    {
        var transacao = await transacoes.GetByIdAsync(transacaoId, ct);
        if (transacao is null) return Result<TransacaoResponse>.Fail("Transação não encontrada.");
        if (transacao.Status != StatusTransacao.Pendente) return Result<TransacaoResponse>.Fail("Transação já processada.");

        var cartaoInput = new CartaoInput(request.Numero, request.Nome, request.Validade, request.Cvv);
        var resultado = await gateway.ProcessarAsync(transacao.Id, transacao.Valor, transacao.Metodo, cartaoInput, ct);

        transacao.Status = resultado.Aprovado ? StatusTransacao.Aprovado : StatusTransacao.Recusado;
        transacao.OperadoraRef = resultado.Referencia;
        transacao.CartaoUltimos4 = resultado.Ultimos4;
        transacao.CartaoBandeira = resultado.Bandeira;
        transacao.MensagemErro = resultado.Erro;
        transacao.AtualizadoEm = clock.UtcNow;

        await transacoes.SaveChangesAsync(ct);
        logger.LogInformation("Cartão processado. TransacaoId={Id} Aprovado={Aprovado}", transacaoId, resultado.Aprovado);
        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }
}
