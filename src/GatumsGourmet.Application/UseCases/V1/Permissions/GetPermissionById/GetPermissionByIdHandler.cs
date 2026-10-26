using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Permissions.GetPermissionById;

public interface IGetPermissionByIdHandler
{
    Task<Result<PermissionResponse>> Handle(int id, CancellationToken ct = default);
}

public sealed class GetPermissionByIdHandler(IPermissionRepository permissions) : IGetPermissionByIdHandler
{
    public async Task<Result<PermissionResponse>> Handle(int id, CancellationToken ct = default)
    {
        var p = await permissions.GetByIdAsync(id, ct);
        if (p is null) return Result<PermissionResponse>.Fail("Permissão não encontrada.");
        return Result<PermissionResponse>.Ok(new PermissionResponse(p.Id, p.Codigo, p.Descricao, p.Recurso, p.Acao));
    }
}
