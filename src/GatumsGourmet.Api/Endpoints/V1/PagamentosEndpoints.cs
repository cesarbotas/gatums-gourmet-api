using GatumsGourmet.Api.Authorization;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ConfirmarPagamentoPix;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ConsultarStatusPagamento;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPagamento;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPix;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ListOpcoesPagamento;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoCredito;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoDebito;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarPagamentoCartao;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarWebhookMercadoPago;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ValidarPagamentoParaAprovacao;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1;

public static class PagamentosEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        // ── Opções disponíveis ──────────────────────────────────
        group.MapGet("/opcoes", async (IListOpcoesPagamentoHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttpResult())
            .RequireAuthorization();

        // ── FLUXO PIX (Mercado Pago / Provedor Oficial) ─────────
        group.MapPost("/pix", async (
            [FromBody] IniciarPixRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            IIniciarPixHandler handler,
            CancellationToken ct) =>
            (await handler.Handle(request, idempotencyKey, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequireAuthorization();

        group.MapGet("/pix/{transacaoId:guid}/status", async (
            Guid transacaoId,
            IConsultarStatusPagamentoHandler handler,
            CancellationToken ct) =>
            (await handler.Handle(transacaoId, ct)).ToHttpResult())
            .RequireAuthorization();

        // Webhook público do Mercado Pago para notificação de pagamentos em tempo real
        group.MapPost("/webhook/mercadopago", async (
            [FromQuery(Name = "id")] string? queryId,
            [FromQuery(Name = "data.id")] string? dataId,
            [FromBody] MercadoPagoWebhookBody? body,
            IProcessarWebhookMercadoPagoHandler handler,
            CancellationToken ct) =>
        {
            var id = !string.IsNullOrWhiteSpace(queryId) ? queryId : dataId;
            return (await handler.Handle(id, body, ct)).ToHttpResult();
        }).AllowAnonymous();

        // ── FLUXO CARTÃO DE CRÉDITO ─────────────────────────────
        group.MapPost("/cartao-credito", async (
            [FromBody] ProcessarCartaoCreditoRequest request,
            IProcessarCartaoCreditoHandler handler,
            CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult())
            .RequireAuthorization();

        // ── FLUXO CARTÃO DE DÉBITO ──────────────────────────────
        group.MapPost("/cartao-debito", async (
            [FromBody] ProcessarCartaoDebitoRequest request,
            IProcessarCartaoDebitoHandler handler,
            CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult())
            .RequireAuthorization();

        // ── FLUXOS LEGADOS / COMPATIBILIDADE ────────────────────
        group.MapPost("/iniciar", async (
            [FromBody] IniciarPagamentoRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            IIniciarPagamentoHandler handler,
            CancellationToken ct) =>
            (await handler.Handle(request, idempotencyKey, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequireAuthorization();

        group.MapPost("/{transacaoId:guid}/cartao", async (
            Guid transacaoId,
            [FromBody] ProcessarPagamentoCartaoRequest request,
            IProcessarPagamentoCartaoHandler handler,
            CancellationToken ct) =>
            (await handler.Handle(transacaoId, request, ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapGet("/{transacaoId:guid}/status", async (
            Guid transacaoId,
            IConsultarStatusPagamentoHandler handler,
            CancellationToken ct) =>
            (await handler.Handle(transacaoId, ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapPost("/{transacaoId:guid}/pix/confirmar", async (
            Guid transacaoId,
            IConfirmarPagamentoPixHandler handler,
            CancellationToken ct) =>
            (await handler.Handle(transacaoId, ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapPost("/{transacaoId:guid}/validar", async (
            Guid transacaoId,
            IValidarPagamentoParaAprovacaoHandler handler,
            CancellationToken ct) =>
            (await handler.Handle(transacaoId, ct)).ToHttpResult())
            .RequirePermission("pagamentos:validar");
    }
}
