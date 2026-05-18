using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.Produtos.CreateProduto;
using GatumsGourmet.Application.UseCases.Produtos.DeleteProduto;
using GatumsGourmet.Application.UseCases.Produtos.GetProdutoById;
using GatumsGourmet.Application.UseCases.Produtos.ListCategorias;
using GatumsGourmet.Application.UseCases.Produtos.ListProdutos;
using GatumsGourmet.Application.UseCases.Produtos.UpdateProduto;
using GatumsGourmet.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints;

public static class ProdutosEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/categorias", async (IListCategoriasHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttpResult());

        group.MapGet("/", async (
            [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string? search,
            [FromQuery] string? categoria, [FromQuery] TipoProduto? tipo, [FromQuery] bool? disponivel,
            IListProdutosHandler handler, CancellationToken ct) =>
            (await handler.Handle(new ListProdutosRequest(page < 1 ? 1 : page, pageSize < 1 ? 10 : pageSize, search, categoria, tipo, disponivel), ct)).ToHttpResult());

        group.MapGet("/{id:guid}", async (Guid id, IGetProdutoByIdHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult());

        group.MapPost("/", async ([FromBody] CreateProdutoRequest request, ICreateProdutoHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequireAuthorization("AdminOnly");

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateProdutoRequest request, IUpdateProdutoHandler handler, CancellationToken ct) =>
            (await handler.Handle(request with { Id = id }, ct)).ToHttpResult())
            .RequireAuthorization("AdminOnly");

        group.MapDelete("/{id:guid}", async (Guid id, IDeleteProdutoHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult())
            .RequireAuthorization("AdminOnly");
    }
}
