using GatumsGourmet.Api.Authorization;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Produtos.CreateProduto;
using GatumsGourmet.Application.UseCases.V1.Produtos.DeleteProduto;
using GatumsGourmet.Application.UseCases.V1.Produtos.GetProdutoById;
using GatumsGourmet.Application.UseCases.V1.Produtos.ListProdutos;
using GatumsGourmet.Application.UseCases.V1.Produtos.UpdateProduto;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Produtos;

public static class ProdutosEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            [AsParameters] ProdutosRequest request,
            IListProdutosHandler handler,
            CancellationToken ct) =>
            (
                await handler.Handle(new ListProdutosRequest(
                    request.Pagina is null or < 1 ? 1 : request.Pagina.Value,
                    request.Tamanho is null or < 1 ? 10 : request.Tamanho.Value,
                    request.Search,
                    request.CategoriaId,
                    request.Tipo,
                    request.Disponivel), ct))
                .ToHttpResult());

        group.MapGet("/{id:guid}", async (Guid id, IGetProdutoByIdHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult());

        group.MapPost("/", async ([FromBody] CreateProdutoRequest request, ICreateProdutoHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequirePermission("produtos:escrever");

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateProdutoRequest request, IUpdateProdutoHandler handler, CancellationToken ct) =>
            (await handler.Handle(request with { Id = id }, ct)).ToHttpResult())
            .RequirePermission("produtos:escrever");

        group.MapDelete("/{id:guid}", async (Guid id, IDeleteProdutoHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult())
            .RequirePermission("produtos:escrever");
    }
}
