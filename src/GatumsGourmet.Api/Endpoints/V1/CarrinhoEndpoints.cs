using System.Security.Claims;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Carrinho.AddCarrinhoItem;
using GatumsGourmet.Application.UseCases.V1.Carrinho.ClearCarrinho;
using GatumsGourmet.Application.UseCases.V1.Carrinho.GetCarrinho;
using GatumsGourmet.Application.UseCases.V1.Carrinho.RemoveCarrinhoItem;
using GatumsGourmet.Application.UseCases.V1.Carrinho.UpdateCarrinhoItemQuantidade;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1;

public static class CarrinhoEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (ClaimsPrincipal user, IGetCarrinhoHandler handler, CancellationToken ct) =>
            (await handler.Handle(user.GetUserId(), ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapPost("/itens", async (ClaimsPrincipal user, [FromBody] AddCarrinhoItemRequest request, IAddCarrinhoItemHandler handler, CancellationToken ct) =>
            (await handler.Handle(user.GetUserId(), request, ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapPatch("/itens/{produtoId:guid}", async (ClaimsPrincipal user, Guid produtoId, [FromBody] UpdateCarrinhoItemQuantidadeRequest request, IUpdateCarrinhoItemQuantidadeHandler handler, CancellationToken ct) =>
            (await handler.Handle(user.GetUserId(), produtoId, request.Quantidade, ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapDelete("/itens/{produtoId:guid}", async (ClaimsPrincipal user, Guid produtoId, IRemoveCarrinhoItemHandler handler, CancellationToken ct) =>
            (await handler.Handle(user.GetUserId(), produtoId, ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapDelete("/", async (ClaimsPrincipal user, IClearCarrinhoHandler handler, CancellationToken ct) =>
            (await handler.Handle(user.GetUserId(), ct)).ToHttpResult())
            .RequireAuthorization();
    }
}
