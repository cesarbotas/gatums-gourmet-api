using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.Categorias.CreateCategoria;
using GatumsGourmet.Application.UseCases.Categorias.DeleteCategoria;
using GatumsGourmet.Application.UseCases.Categorias.GetCategoriaById;
using GatumsGourmet.Application.UseCases.Categorias.ListCategorias;
using GatumsGourmet.Application.UseCases.Categorias.UpdateCategoria;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints;

public static class CategoriasEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        // Listagem pública. ?apenasAtivas=true para trazer só as ativas (uso típico do catálogo).
        group.MapGet("/", async ([FromQuery] bool? apenasAtivas, IListCategoriasHandler handler, CancellationToken ct) =>
            (await handler.Handle(apenasAtivas, ct)).ToHttpResult());

        group.MapGet("/{id:long}", async (long id, IGetCategoriaByIdHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult());

        group.MapPost("/", async ([FromBody] CreateCategoriaRequest request, ICreateCategoriaHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequireAuthorization("AdminOnly");

        group.MapPut("/{id:long}", async (long id, [FromBody] UpdateCategoriaRequest request, IUpdateCategoriaHandler handler, CancellationToken ct) =>
            (await handler.Handle(request with { Id = id }, ct)).ToHttpResult())
            .RequireAuthorization("AdminOnly");

        group.MapDelete("/{id:long}", async (long id, IDeleteCategoriaHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult())
            .RequireAuthorization("AdminOnly");
    }
}
