using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Roles.DeleteRole;

public interface IDeleteRoleHandler
{
    Task<Result<bool>> Handle(int id, CancellationToken ct = default);
}

public sealed class DeleteRoleHandler(IRoleRepository roles) : IDeleteRoleHandler
{
    public async Task<Result<bool>> Handle(int id, CancellationToken ct = default)
    {
        var role = await roles.GetByIdAsync(id, ct);
        if (role is null) return Result<bool>.Fail("Role não encontrada.");
        if (role.Sistema) return Result<bool>.Fail("Roles de sistema não podem ser removidas.");

        if (await roles.CountUsuariosAsync(id, ct) > 0)
            return Result<bool>.Fail("Role possui usuários vinculados.");

        roles.Remove(role);
        await roles.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}
