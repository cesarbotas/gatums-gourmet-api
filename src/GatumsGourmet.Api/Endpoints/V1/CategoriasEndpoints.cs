using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Categorias.CreateCategoria;
using GatumsGourmet.Application.UseCases.V1.Categorias.DeleteCategoria;
using GatumsGourmet.Application.UseCases.V1.Categorias.GetCategoriaById;
using GatumsGourmet.Application.UseCases.V1.Categorias.ListCategorias;
using GatumsGourmet.Application.UseCases.V1.Categorias.UpdateCategoria;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1;

public static class CategoriasEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        // Listagem pública paginada. ?apenasAtivas=true para trazer só as ativas (catálogo).
        group.MapGet("/", async (
            [FromQuery] int pagina, [FromQuery] int tamanho, [FromQuery] bool? apenasAtivas,
            IListCategoriasHandler handler, CancellationToken ct) =>
            (await handler.Handle(new ListCategoriasRequest(pagina < 1 ? 1 : pagina, tamanho < 1 ? 10 : tamanho, apenasAtivas), ct)).ToHttpResult());

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
