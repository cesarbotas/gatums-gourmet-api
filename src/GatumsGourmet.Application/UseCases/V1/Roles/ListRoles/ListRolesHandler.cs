using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Roles.ListRoles;

public interface IListRolesHandler
{
    Task<Result<IReadOnlyList<RoleResponse>>> Handle(CancellationToken ct = default);
}

public sealed class ListRolesHandler(IRoleRepository roles) : IListRolesHandler
{
    public async Task<Result<IReadOnlyList<RoleResponse>>> Handle(CancellationToken ct = default)
    {
        var items = await roles.ListAsync(ct);
        IReadOnlyList<RoleResponse> dtos = items
            .Select(r => new RoleResponse(
                r.Id,
                r.Nome,
                r.Descricao,
                r.Sistema,
                r.RolePermissions.Select(rp => rp.Permission.Codigo).OrderBy(c => c).ToList()))
            .ToList();
        return Result<IReadOnlyList<RoleResponse>>.Ok(dtos);
    }
}
