using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Permissions.ListPermissions;

public interface IListPermissionsHandler
{
    Task<Result<IReadOnlyList<PermissionResponse>>> Handle(CancellationToken ct = default);
}

public sealed class ListPermissionsHandler(IPermissionRepository permissions) : IListPermissionsHandler
{
    public async Task<Result<IReadOnlyList<PermissionResponse>>> Handle(CancellationToken ct = default)
    {
        var items = await permissions.ListAsync(ct);
        IReadOnlyList<PermissionResponse> dtos = items
            .Select(p => new PermissionResponse(p.Id, p.Codigo, p.Descricao, p.Recurso, p.Acao))
            .ToList();
        return Result<IReadOnlyList<PermissionResponse>>.Ok(dtos);
    }
}
