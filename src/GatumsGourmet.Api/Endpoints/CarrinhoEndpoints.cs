using System.Security.Claims;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.Carrinho.AddCarrinhoItem;
using GatumsGourmet.Application.UseCases.Carrinho.ClearCarrinho;
using GatumsGourmet.Application.UseCases.Carrinho.GetCarrinho;
using GatumsGourmet.Application.UseCases.Carrinho.RemoveCarrinhoItem;
using GatumsGourmet.Application.UseCases.Carrinho.UpdateCarrinhoItemQuantidade;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints;

public static class CarrinhoEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (ClaimsPrincipal user, IGetCarrinhoHandler handler, CancellationToken ct) =>
            (await handler.Handle(user.GetUserId(), ct)).ToHttpResult())
            .RequireAuthorization("ClienteOnly");

        group.MapPost("/itens", async (ClaimsPrincipal user, [FromBody] AddCarrinhoItemRequest request, IAddCarrinhoItemHandler handler, CancellationToken ct) =>
            (await handler.Handle(user.GetUserId(), request, ct)).ToHttpResult())
            .RequireAuthorization("ClienteOnly");

        group.MapPatch("/itens/{produtoId:guid}", async (ClaimsPrincipal user, Guid produtoId, [FromBody] UpdateCarrinhoItemQuantidadeRequest request, IUpdateCarrinhoItemQuantidadeHandler handler, CancellationToken ct) =>
            (await handler.Handle(user.GetUserId(), produtoId, request.Quantidade, ct)).ToHttpResult())
            .RequireAuthorization("ClienteOnly");

        group.MapDelete("/itens/{produtoId:guid}", async (ClaimsPrincipal user, Guid produtoId, IRemoveCarrinhoItemHandler handler, CancellationToken ct) =>
            (await handler.Handle(user.GetUserId(), produtoId, ct)).ToHttpResult())
            .RequireAuthorization("ClienteOnly");

        group.MapDelete("/", async (ClaimsPrincipal user, IClearCarrinhoHandler handler, CancellationToken ct) =>
            (await handler.Handle(user.GetUserId(), ct)).ToHttpResult())
            .RequireAuthorization("ClienteOnly");
    }
}
