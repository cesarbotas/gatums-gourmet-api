using System.Security.Claims;
using GatumsGourmet.Api.Authorization;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.CriarVendaPresencial;
using GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.ConfirmarPagamentoPresencialCartao;
using GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.RegistrarPagamentoPresencial;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Admin;

/// <summary>
/// Endpoints exclusivos do canal presencial (tela /admin/venda-rapida).
/// Todos exigem permissão <c>vendas_presenciais:criar</c>.
/// </summary>
public static class VendaPresencialEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        // POST /admin/vendas-presenciais
        // Cria pedido presencial: valida estoque, calcula total, decrementa estoque atomicamente.
        group.MapPost("/", async (
            ClaimsPrincipal user,
            [FromBody] CriarVendaPresencialBodyRequest body,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            ICriarVendaPresencialHandler handler,
            CancellationToken ct) =>
        {
            var operadorId = user.GetUserId();
            var request = new CriarVendaPresencialRequest(
                body.ClienteNome,
                operadorId,          // OperadorId sempre vem do JWT, nunca do body
                body.Itens,
                idempotencyKey);
            return (await handler.Handle(request, ct)).ToHttpResult(StatusCodes.Status201Created);
        })
        .RequirePermission("vendas_presenciais:criar");

        // POST /admin/vendas-presenciais/pagamento
        // Registra pagamento de um pedido presencial (dinheiro/cartão/Pix).
        group.MapPost("/pagamento", async (
            [FromBody] RegistrarPagamentoPresencialRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            IRegistrarPagamentoPresencialHandler handler,
            CancellationToken ct) =>
        {
            var req = idempotencyKey is not null ? request with { IdempotencyKey = idempotencyKey } : request;
            return (await handler.Handle(req, ct)).ToHttpResult(StatusCodes.Status201Created);
        })
        .RequirePermission("vendas_presenciais:criar");

        // POST /admin/vendas-presenciais/pagamento/{transacaoId}/confirmar-cartao
        // Operador confirma (ou recusa) o cartão após a maquininha processar.
        group.MapPost("/pagamento/{transacaoId:guid}/confirmar-cartao", async (
            Guid transacaoId,
            [FromBody] ConfirmarPagamentoPresencialCartaoRequest request,
            IConfirmarPagamentoPresencialCartaoHandler handler,
            CancellationToken ct) =>
        (await handler.Handle(transacaoId, request, ct)).ToHttpResult())
        .RequirePermission("vendas_presenciais:criar");
    }
}

/// <summary>Body do POST /admin/vendas-presenciais — operadorId vem do JWT, não do body.</summary>
public record CriarVendaPresencialBodyRequest(
    string? ClienteNome,
    IReadOnlyList<CriarVendaPresencialItemRequest> Itens);
