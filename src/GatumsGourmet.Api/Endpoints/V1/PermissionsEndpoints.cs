using GatumsGourmet.Api.Authorization;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Permissions.CreatePermission;
using GatumsGourmet.Application.UseCases.V1.Permissions.DeletePermission;
using GatumsGourmet.Application.UseCases.V1.Permissions.GetPermissionById;
using GatumsGourmet.Application.UseCases.V1.Permissions.ListPermissions;
using GatumsGourmet.Application.UseCases.V1.Permissions.UpdatePermission;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1;

public static class PermissionsEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (IListPermissionsHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttpResult())
            .RequirePermission("permissoes:gerenciar");

        group.MapGet("/{id:int}", async (int id, IGetPermissionByIdHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult())
            .RequirePermission("permissoes:gerenciar");

        group.MapPost("/", async ([FromBody] CreatePermissionRequest request, ICreatePermissionHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequirePermission("permissoes:gerenciar");

        group.MapPut("/{id:int}", async (int id, [FromBody] UpdatePermissionRequest request, IUpdatePermissionHandler handler, CancellationToken ct) =>
            (await handler.Handle(request with { Id = id }, ct)).ToHttpResult())
            .RequirePermission("permissoes:gerenciar");

        group.MapDelete("/{id:int}", async (int id, IDeletePermissionHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult())
            .RequirePermission("permissoes:gerenciar");
    }
}
