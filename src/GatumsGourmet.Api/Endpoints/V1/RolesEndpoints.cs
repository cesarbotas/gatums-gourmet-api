using GatumsGourmet.Api.Authorization;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Roles.CreateRole;
using GatumsGourmet.Application.UseCases.V1.Roles.DeleteRole;
using GatumsGourmet.Application.UseCases.V1.Roles.GetRoleById;
using GatumsGourmet.Application.UseCases.V1.Roles.ListRoles;
using GatumsGourmet.Application.UseCases.V1.Roles.SetRolePermissions;
using GatumsGourmet.Application.UseCases.V1.Roles.UpdateRole;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1;

public static class RolesEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (IListRolesHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttpResult())
            .RequirePermission("roles:gerenciar");

        group.MapGet("/{id:int}", async (int id, IGetRoleByIdHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult())
            .RequirePermission("roles:gerenciar");

        group.MapPost("/", async ([FromBody] CreateRoleRequest request, ICreateRoleHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult(StatusCodes.Status201Created))
            .RequirePermission("roles:gerenciar");

        group.MapPut("/{id:int}", async (int id, [FromBody] UpdateRoleRequest request, IUpdateRoleHandler handler, CancellationToken ct) =>
            (await handler.Handle(request with { Id = id }, ct)).ToHttpResult())
            .RequirePermission("roles:gerenciar");

        group.MapDelete("/{id:int}", async (int id, IDeleteRoleHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult())
            .RequirePermission("roles:gerenciar");

        group.MapPut("/{id:int}/permissions", async (int id, [FromBody] SetRolePermissionsRequest request, ISetRolePermissionsHandler handler, CancellationToken ct) =>
            (await handler.Handle(request with { RoleId = id }, ct)).ToHttpResult())
            .RequirePermission("roles:gerenciar");
    }
}
