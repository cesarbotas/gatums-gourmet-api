using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Roles.SetRolePermissions;

public record SetRolePermissionsRequest(int RoleId, IReadOnlyList<int> PermissionIds);

public interface ISetRolePermissionsHandler
{
    Task<Result<bool>> Handle(SetRolePermissionsRequest request, CancellationToken ct = default);
}

public sealed class SetRolePermissionsHandler(IRoleRepository roles) : ISetRolePermissionsHandler
{
    public async Task<Result<bool>> Handle(SetRolePermissionsRequest request, CancellationToken ct = default)
    {
        var role = await roles.GetByIdAsync(request.RoleId, ct);
        if (role is null) return Result<bool>.Fail("Role não encontrada.");

        // Decisão de segurança (ver Task 12 report): o conjunto de permissões de
        // roles de sistema (Admin/Cliente) é fixo e não pode ser alterado via API.
        if (role.Sistema) return Result<bool>.Fail("Permissões de roles de sistema são fixas.");

        await roles.SetPermissionsAsync(request.RoleId, request.PermissionIds ?? [], ct);
        await roles.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}
