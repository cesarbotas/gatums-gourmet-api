using GatumsGourmet.Api.Authorization;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Clientes.CreateCliente;
using GatumsGourmet.Application.UseCases.V1.Clientes.DeleteCliente;
using GatumsGourmet.Application.UseCases.V1.Clientes.ListClientes;
using GatumsGourmet.Application.UseCases.V1.Clientes.UpdateCliente;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Clientes;

public static class ClientesEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            [AsParameters] ClientesRequest request,
            IListClientesHandler handler, CancellationToken ct) =>
            (await handler.Handle(new ListClientesRequest(
                request.Pagina < 1 ? 1 : request.Pagina,
                request.Tamanho < 1 ? 10 : request.Tamanho,
                request.Search), ct)).ToHttpResult())
            .RequirePermission("clientes:ler");

        group.MapPost("/", async ([FromBody] CreateClienteRequest request, ICreateClienteHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequirePermission("clientes:escrever");

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateClienteRequest request, IUpdateClienteHandler handler, CancellationToken ct) =>
            (await handler.Handle(request with { Id = id }, ct)).ToHttpResult())
            .RequirePermission("clientes:escrever");

        group.MapDelete("/{id:guid}", async (Guid id, IDeleteClienteHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult())
            .RequirePermission("clientes:escrever");
    }
}
