using System.Text.Json;
using GatumsGourmet.Application.UseCases.V1.Pagamentos;
using GatumsGourmet.Application.UseCases.V2.Pagamentos.IniciarPix;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V2.Pagamentos.ConsultarStatus;

public interface IConsultarStatusV2Handler
{
    Task<Result<TransacaoResponse>> Handle(Guid transacaoId, CancellationToken ct = default);
}

public sealed class ConsultarStatusV2Handler(
    ITransacaoRepository transacoes,
    IPixOrdersProvider provider,
    IAplicarResultadoPagamentoPixService aplicarResultado,
    ILogger<ConsultarStatusV2Handler> logger) : IConsultarStatusV2Handler
{
    private const string Origem = "Consulta de status (Orders)";

    public async Task<Result<TransacaoResponse>> Handle(Guid transacaoId, CancellationToken ct = default)
    {
        var transacao = await transacoes.GetByIdAsync(transacaoId, ct);
        if (transacao is null) return Result<TransacaoResponse>.Fail("Transação não encontrada.");

        if (transacao.Status == StatusTransacao.Pendente
            && transacao.Metodo == MetodoPagamento.Pix
            && transacao.Operadora == IniciarPixV2Handler.OperadoraOrders
            && !string.IsNullOrWhiteSpace(transacao.OperadoraRef))
        {
            // Best effort: falha do provedor não derruba a consulta; a transação segue Pendente.
            try
            {
                var status = await provider.ConsultarOrderAsync(transacao.OperadoraRef, ct);
                if (status is not null)
                    await aplicarResultado.AplicarAsync(transacao, status, Origem, ct);
            }
            catch (Exception ex) when (ex is PixOrdersProviderException or JsonException)
            {
                logger.LogWarning(ex,
                    "Falha ao consultar order no Mercado Pago; mantendo transação pendente. OrderId={OrderId} TransacaoId={TransacaoId}",
                    transacao.OperadoraRef, transacao.Id);
            }
        }

        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }
}
