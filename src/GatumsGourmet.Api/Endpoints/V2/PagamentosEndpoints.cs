using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPix;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ListOpcoesPagamento;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoCredito;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoDebito;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarWebhookMercadoPago;
using GatumsGourmet.Application.UseCases.V2.Pagamentos.ConsultarStatus;
using GatumsGourmet.Application.UseCases.V2.Pagamentos.IniciarPix;
using GatumsGourmet.Application.UseCases.V2.Pagamentos.ProcessarWebhookOrders;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V2;

public static class PagamentosEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        // ── Opções disponíveis (reaproveita a V1) ───────────────
        group.MapGet("/opcoes", async (IListOpcoesPagamentoHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttpResult())
            .RequireAuthorization();

        // ── PIX via Mercado Pago Orders API ─────────────────────
        group.MapPost("/pix", async (
            [FromBody] IniciarPixRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            IIniciarPixV2Handler handler,
            CancellationToken ct) =>
            (await handler.Handle(request, idempotencyKey, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequireAuthorization();

        group.MapGet("/pix/{transacaoId:guid}/status", async (
            Guid transacaoId,
            IConsultarStatusV2Handler handler,
            CancellationToken ct) =>
            (await handler.Handle(transacaoId, ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapGet("/{transacaoId:guid}/status", async (
            Guid transacaoId,
            IConsultarStatusV2Handler handler,
            CancellationToken ct) =>
            (await handler.Handle(transacaoId, ct)).ToHttpResult())
            .RequireAuthorization();

        // Webhook público do Mercado Pago (Orders): valida x-signature antes de processar.
        group.MapPost("/webhook/mercadopago", async (
            HttpRequest httpRequest,
            [FromQuery(Name = "data.id")] string? queryDataId,
            [FromQuery(Name = "type")] string? queryType,
            [FromBody] MercadoPagoWebhookBody? body,
            IMercadoPagoWebhookSignatureValidator validator,
            IProcessarWebhookOrdersHandler handler,
            CancellationToken ct) =>
        {
            var dataId = !string.IsNullOrWhiteSpace(queryDataId) ? queryDataId : body?.Data?.Id;
            var type = !string.IsNullOrWhiteSpace(queryType) ? queryType : body?.Type;

            var signature = httpRequest.Headers["x-signature"].ToString();
            var requestId = httpRequest.Headers["x-request-id"].ToString();

            if (!validator.IsValid(
                    string.IsNullOrEmpty(signature) ? null : signature,
                    string.IsNullOrEmpty(requestId) ? null : requestId,
                    dataId))
                return Results.Unauthorized();

            return (await handler.Handle(dataId, type, ct)).ToHttpResult();
        }).AllowAnonymous();

        // ── Cartões (reaproveita a V1) ──────────────────────────
        group.MapPost("/cartao-credito", async (
            [FromBody] ProcessarCartaoCreditoRequest request,
            IProcessarCartaoCreditoHandler handler,
            CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapPost("/cartao-debito", async (
            [FromBody] ProcessarCartaoDebitoRequest request,
            IProcessarCartaoDebitoHandler handler,
            CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult())
            .RequireAuthorization();
    }
}
