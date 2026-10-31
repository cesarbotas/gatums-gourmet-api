using GatumsGourmet.Api.Authorization;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Categorias.CreateCategoria;
using GatumsGourmet.Application.UseCases.V1.Categorias.DeleteCategoria;
using GatumsGourmet.Application.UseCases.V1.Categorias.GetCategoriaById;
using GatumsGourmet.Application.UseCases.V1.Categorias.ListCategorias;
using GatumsGourmet.Application.UseCases.V1.Categorias.UpdateCategoria;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Categorias;

public static class CategoriasEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        // Listagem pública paginada. ?apenasAtivas=true para trazer só as ativas (catálogo).
        group.MapGet("/", async (
            [AsParameters] CategoriasRequest request,
            IListCategoriasHandler handler, CancellationToken ct) =>
            (await handler.Handle(new ListCategoriasRequest(
                request.Pagina is null or < 1 ? 1 : request.Pagina.Value,
                request.Tamanho is null or < 1 ? 10 : request.Tamanho.Value,
                request.ApenasAtivas), ct)).ToHttpResult());

        group.MapGet("/{id:long}", async (long id, IGetCategoriaByIdHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult());

        group.MapPost("/", async ([FromBody] CreateCategoriaRequest request, ICreateCategoriaHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequirePermission("categorias:escrever");

        group.MapPut("/{id:long}", async (long id, [FromBody] UpdateCategoriaRequest request, IUpdateCategoriaHandler handler, CancellationToken ct) =>
            (await handler.Handle(request with { Id = id }, ct)).ToHttpResult())
            .RequirePermission("categorias:escrever");

        group.MapDelete("/{id:long}", async (long id, IDeleteCategoriaHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult())
            .RequirePermission("categorias:escrever");
    }
}
