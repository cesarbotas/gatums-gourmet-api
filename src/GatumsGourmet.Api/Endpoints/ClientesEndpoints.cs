using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.Clientes.CreateCliente;
using GatumsGourmet.Application.UseCases.Clientes.DeleteCliente;
using GatumsGourmet.Application.UseCases.Clientes.ListClientes;
using GatumsGourmet.Application.UseCases.Clientes.UpdateCliente;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints;

public static class ClientesEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async ([FromQuery] int pagina, [FromQuery] int tamanho, [FromQuery] string? search, IListClientesHandler handler, CancellationToken ct) =>
            (await handler.Handle(new ListClientesRequest(pagina < 1 ? 1 : pagina, tamanho < 1 ? 10 : tamanho, search), ct)).ToHttpResult())
            .RequireAuthorization("AdminOnly");

        group.MapPost("/", async ([FromBody] CreateClienteRequest request, ICreateClienteHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequireAuthorization("AdminOnly");

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateClienteRequest request, IUpdateClienteHandler handler, CancellationToken ct) =>
            (await handler.Handle(request with { Id = id }, ct)).ToHttpResult())
            .RequireAuthorization("AdminOnly");

        group.MapDelete("/{id:guid}", async (Guid id, IDeleteClienteHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult())
            .RequireAuthorization("AdminOnly");
    }
}
