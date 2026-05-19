using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.Pagamentos.ConfirmarPagamentoPix;
using GatumsGourmet.Application.UseCases.Pagamentos.ConsultarStatusPagamento;
using GatumsGourmet.Application.UseCases.Pagamentos.IniciarPagamento;
using GatumsGourmet.Application.UseCases.Pagamentos.ListOpcoesPagamento;
using GatumsGourmet.Application.UseCases.Pagamentos.ProcessarPagamentoCartao;
using GatumsGourmet.Application.UseCases.Pagamentos.ValidarPagamentoParaAprovacao;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints;

public static class PagamentosEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/opcoes", async (IListOpcoesPagamentoHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttpResult())
            .RequireAuthorization("ClienteOnly");

        group.MapPost("/iniciar", async ([FromBody] IniciarPagamentoRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, IIniciarPagamentoHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, idempotencyKey, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequireAuthorization("ClienteOnly");

        group.MapPost("/{transacaoId:guid}/cartao", async (Guid transacaoId, [FromBody] ProcessarPagamentoCartaoRequest request, IProcessarPagamentoCartaoHandler handler, CancellationToken ct) =>
            (await handler.Handle(transacaoId, request, ct)).ToHttpResult())
            .RequireAuthorization("ClienteOnly");

        group.MapGet("/{transacaoId:guid}/status", async (Guid transacaoId, IConsultarStatusPagamentoHandler handler, CancellationToken ct) =>
            (await handler.Handle(transacaoId, ct)).ToHttpResult())
            .RequireAuthorization("ClienteOnly");

        group.MapPost("/{transacaoId:guid}/pix/confirmar", async (Guid transacaoId, IConfirmarPagamentoPixHandler handler, CancellationToken ct) =>
            (await handler.Handle(transacaoId, ct)).ToHttpResult())
            .RequireAuthorization("ClienteOnly");

        group.MapPost("/{transacaoId:guid}/validar", async (Guid transacaoId, IValidarPagamentoParaAprovacaoHandler handler, CancellationToken ct) =>
            (await handler.Handle(transacaoId, ct)).ToHttpResult())
            .RequireAuthorization("AdminOnly");
    }
}
