using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Roles.GetRoleById;

public interface IGetRoleByIdHandler
{
    Task<Result<RoleResponse>> Handle(int id, CancellationToken ct = default);
}

public sealed class GetRoleByIdHandler(IRoleRepository roles) : IGetRoleByIdHandler
{
    public async Task<Result<RoleResponse>> Handle(int id, CancellationToken ct = default)
    {
        var role = await roles.GetByIdAsync(id, ct);
        if (role is null) return Result<RoleResponse>.Fail("Role não encontrada.");

        var dto = new RoleResponse(
            role.Id,
            role.Nome,
            role.Descricao,
            role.Sistema,
            role.RolePermissions.Select(rp => rp.Permission.Codigo).OrderBy(c => c).ToList());
        return Result<RoleResponse>.Ok(dto);
    }
}
