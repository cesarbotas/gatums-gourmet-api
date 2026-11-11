using System.Text.Json.Serialization;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarWebhookMercadoPago;

public sealed record MercadoPagoWebhookBody
{
    [JsonPropertyName("action")]
    public string? Action { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("data")]
    public MercadoPagoWebhookData? Data { get; init; }
}

public sealed record MercadoPagoWebhookData
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
}

public interface IProcessarWebhookMercadoPagoHandler
{
    Task<Result<bool>> Handle(string? paymentIdParam, MercadoPagoWebhookBody? body, CancellationToken ct = default);
}

public sealed class ProcessarWebhookMercadoPagoHandler(
    ITransacaoRepository transacoes,
    IPedidoRepository pedidos,
    IPixProvider pixProvider,
    IClock clock,
    ILogger<ProcessarWebhookMercadoPagoHandler> logger) : IProcessarWebhookMercadoPagoHandler
{
    public async Task<Result<bool>> Handle(string? paymentIdParam, MercadoPagoWebhookBody? body, CancellationToken ct = default)
    {
        // O Mercado Pago pode enviar o ID via query string (id ou data.id) ou no corpo JSON
        var paymentId = !string.IsNullOrWhiteSpace(paymentIdParam)
            ? paymentIdParam
            : body?.Data?.Id;

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            logger.LogInformation("Notificação de Webhook do Mercado Pago recebida sem Payment ID. Ignorando.");
            return Result<bool>.Ok(true);
        }

        logger.LogInformation("Processando Webhook Mercado Pago para PaymentId={PaymentId}", paymentId);

        // Consulta a API do Mercado Pago para verificar a autenticidade e status real
        var status = await pixProvider.ConsultarStatusAsync(paymentId, ct);
        logger.LogInformation("Status consultado no Mercado Pago para PaymentId={PaymentId}: {Status}", paymentId, status);

        if (string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase))
        {
            var transacao = await transacoes.GetByOperadoraRefAsync(paymentId, ct);
            if (transacao is null)
            {
                logger.LogWarning("Transação não encontrada para OperadoraRef={PaymentId}", paymentId);
                return Result<bool>.Ok(true);
            }

            if (transacao.Status == StatusTransacao.Pendente)
            {
                transacao.Status = StatusTransacao.Aprovado;
                transacao.AtualizadoEm = clock.UtcNow;

                var pedido = await pedidos.GetByIdAsync(transacao.PedidoId, ct);
                if (pedido is not null && pedido.StatusAtual is PedidoEtapa.Criado or PedidoEtapa.Aprovado)
                {
                    pedido.Pagamento = new PedidoPagamento
                    {
                        PedidoId = pedido.Id,
                        TransacaoId = transacao.Id,
                        Resumo = "Pix (Mercado Pago)",
                        AprovadoEm = clock.UtcNow
                    };
                    pedido.StatusAtual = PedidoEtapa.Pagamento;
                    pedido.Timeline.Add(new PedidoTimelineEvento
                    {
                        Id = Guid.NewGuid(),
                        PedidoId = pedido.Id,
                        Etapa = PedidoEtapa.Pagamento,
                        Em = clock.UtcNow,
                        Por = PedidoActor.Cliente,
                        Observacao = "Pagamento aprovado via Webhook Mercado Pago"
                    });
                }

                await transacoes.SaveChangesAsync(ct);
                await pedidos.SaveChangesAsync(ct);

                logger.LogInformation("Transação {TransacaoId} e Pedido {PedidoId} atualizados para APROVADO via Webhook.",
                    transacao.Id, transacao.PedidoId);
            }
        }
        else if (string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            var transacao = await transacoes.GetByOperadoraRefAsync(paymentId, ct);
            if (transacao is not null && transacao.Status == StatusTransacao.Pendente)
            {
                transacao.Status = StatusTransacao.Recusado;
                transacao.AtualizadoEm = clock.UtcNow;
                await transacoes.SaveChangesAsync(ct);
                logger.LogInformation("Transação {TransacaoId} marcada como RECUSADA via Webhook.", transacao.Id);
            }
        }

        return Result<bool>.Ok(true);
    }
}

