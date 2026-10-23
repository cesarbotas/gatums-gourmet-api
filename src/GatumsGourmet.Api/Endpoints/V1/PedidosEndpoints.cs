using System.Security.Claims;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Pedidos.AvancarPedidoEtapa;
using GatumsGourmet.Application.UseCases.V1.Pedidos.CheckoutPedido;
using GatumsGourmet.Application.UseCases.V1.Pedidos.ConcluirPagamentoPedido;
using GatumsGourmet.Application.UseCases.V1.Pedidos.GetPedidoById;
using GatumsGourmet.Application.UseCases.V1.Pedidos.ListPedidos;
using GatumsGourmet.Domain.Enums.Pedidos;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1;

public static class PedidosEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/checkout", async (ClaimsPrincipal user, [FromBody] CheckoutBody body, ICheckoutPedidoHandler handler, CancellationToken ct) =>
            (await handler.Handle(user.GetUserId(), body.ClienteNome, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequireAuthorization("ClienteOnly");

        group.MapGet("/", async (ClaimsPrincipal user, [FromQuery] int pagina, [FromQuery] int tamanho, IListPedidosHandler handler, CancellationToken ct) =>
        {
            Guid? filtro = user.IsAdmin() ? null : user.GetUserId();
            return (await handler.Handle(filtro, new ListPedidosRequest(pagina < 1 ? 1 : pagina, tamanho < 1 ? 10 : tamanho), ct)).ToHttpResult();
        }).RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, IGetPedidoByIdHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, user.GetUserId(), user.IsAdmin(), ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapPost("/{id:guid}/etapas/avancar", async (Guid id, ClaimsPrincipal user, [FromBody] AvancarPedidoEtapaRequest request, IAvancarPedidoEtapaHandler handler, CancellationToken ct) =>
        {
            var actor = user.IsAdmin() ? PedidoActor.Admin : PedidoActor.Cliente;
            return (await handler.Handle(id, request, actor, ct)).ToHttpResult();
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/pagamento/concluir", async (Guid id, [FromBody] ConcluirPagamentoPedidoRequest request, IConcluirPagamentoPedidoHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, request, ct)).ToHttpResult())
            .RequireAuthorization("ClienteOnly");
    }

    private record CheckoutBody(string ClienteNome);
}
