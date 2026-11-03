using GatumsGourmet.Api.Authorization;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ConfirmarPagamentoPix;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ConsultarStatusPagamento;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPagamento;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ListOpcoesPagamento;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarPagamentoCartao;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ValidarPagamentoParaAprovacao;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1;

public static class PagamentosEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/opcoes", async (IListOpcoesPagamentoHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapPost("/iniciar", async ([FromBody] IniciarPagamentoRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, IIniciarPagamentoHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, idempotencyKey, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequireAuthorization();

        group.MapPost("/{transacaoId:guid}/cartao", async (Guid transacaoId, [FromBody] ProcessarPagamentoCartaoRequest request, IProcessarPagamentoCartaoHandler handler, CancellationToken ct) =>
            (await handler.Handle(transacaoId, request, ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapGet("/{transacaoId:guid}/status", async (Guid transacaoId, IConsultarStatusPagamentoHandler handler, CancellationToken ct) =>
            (await handler.Handle(transacaoId, ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapPost("/{transacaoId:guid}/pix/confirmar", async (Guid transacaoId, IConfirmarPagamentoPixHandler handler, CancellationToken ct) =>
            (await handler.Handle(transacaoId, ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapPost("/{transacaoId:guid}/validar", async (Guid transacaoId, IValidarPagamentoParaAprovacaoHandler handler, CancellationToken ct) =>
            (await handler.Handle(transacaoId, ct)).ToHttpResult())
            .RequirePermission("pagamentos:validar");
    }
}
