using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.Produtos.CreateProduto;
using GatumsGourmet.Application.UseCases.Produtos.DeleteProduto;
using GatumsGourmet.Application.UseCases.Produtos.GetProdutoById;
using GatumsGourmet.Application.UseCases.Produtos.ListProdutos;
using GatumsGourmet.Application.UseCases.Produtos.UpdateProduto;
using GatumsGourmet.Domain.Enums.Produtos;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints;

public static class ProdutosEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            [FromQuery] int pagina, [FromQuery] int tamanho, [FromQuery] string? search,
            [FromQuery] long? categoriaId, [FromQuery] TipoProduto? tipo, [FromQuery] bool? disponivel,
            IListProdutosHandler handler, CancellationToken ct) =>
            (await handler.Handle(new ListProdutosRequest(pagina < 1 ? 1 : pagina, tamanho < 1 ? 10 : tamanho, search, categoriaId, tipo, disponivel), ct)).ToHttpResult());

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
