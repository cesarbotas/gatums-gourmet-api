using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V2.Pagamentos.ProcessarWebhookOrders;

public interface IProcessarWebhookOrdersHandler
{
    Task<Result<bool>> Handle(string? dataId, string? type, CancellationToken ct = default);
}

public sealed class ProcessarWebhookOrdersHandler(
    ITransacaoRepository transacoes,
    IPixOrdersProvider provider,
    IAplicarResultadoPagamentoPixService aplicarResultado,
    ILogger<ProcessarWebhookOrdersHandler> logger) : IProcessarWebhookOrdersHandler
{
    private const string Origem = "Webhook Mercado Pago (Orders)";

    public async Task<Result<bool>> Handle(string? dataId, string? type, CancellationToken ct = default)
    {
        if (!string.Equals(type, "order", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(dataId))
        {
            logger.LogInformation("Webhook Orders ignorado (type={Type}, dataIdInformado={TemDataId}).",
                type, !string.IsNullOrWhiteSpace(dataId));
            return Result<bool>.Ok(true);
        }

        // Transação local primeiro: evita chamada desnecessária ao PSP para orders desconhecidas.
        var transacao = await transacoes.GetByOperadoraRefAsync(dataId, ct);
        if (transacao is null)
        {
            logger.LogWarning("Transação não encontrada para OperadoraRef={OrderId}. Ignorando webhook.", dataId);
            return Result<bool>.Ok(true);
        }

        // Não confia no body: confirma o estado real no Mercado Pago.
        // Exceções do provider propagam (ExceptionMiddleware responde erro e o MP reenvia).
        var status = await provider.ConsultarOrderAsync(dataId, ct);
        if (status is null)
        {
            logger.LogWarning("Order {OrderId} não encontrada no Mercado Pago. Nenhuma alteração.", dataId);
            return Result<bool>.Ok(true);
        }

        var alterou = await aplicarResultado.AplicarAsync(transacao, status, Origem, ct);
        logger.LogInformation("Webhook Orders processado. OrderId={OrderId} TransacaoId={TransacaoId} Alterou={Alterou}",
            dataId, transacao.Id, alterou);

        return Result<bool>.Ok(true);
    }
}
