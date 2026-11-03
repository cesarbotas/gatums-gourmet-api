using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Usuarios.SetUsuarioRoles;

public record SetUsuarioRolesRequest(Guid UsuarioId, IReadOnlyList<int> RoleIds);

public interface ISetUsuarioRolesHandler
{
    Task<Result<bool>> Handle(SetUsuarioRolesRequest request, CancellationToken ct = default);
}

public sealed class SetUsuarioRolesHandler(
    IUsuarioRepository usuarios,
    IRoleRepository roles) : ISetUsuarioRolesHandler
{
    public async Task<Result<bool>> Handle(SetUsuarioRolesRequest request, CancellationToken ct = default)
    {
        var usuario = await usuarios.GetByIdComRolesAsync(request.UsuarioId, ct);
        if (usuario is null) return Result<bool>.Fail("Usuário não encontrado.");

        var roleIds = (request.RoleIds ?? []).Distinct().ToList();

        foreach (var roleId in roleIds)
        {
            if (await roles.GetByIdAsync(roleId, ct) is null)
                return Result<bool>.Fail($"Role {roleId} não encontrada.");
        }

        // Regra "nunca sem admin": se o alvo é um admin ativo que está sendo
        // despromovido (as novas roles não incluem Admin) e ele é o único admin
        // ativo do sistema, a operação é bloqueada.
        if (UsuarioMapper.TemRoleAdmin(usuario) && usuario.Ativo)
        {
            var adminRole = await roles.GetByNomeAsync(UsuarioMapper.AdminRoleNome, ct);
            var mantemAdmin = adminRole is not null && roleIds.Contains(adminRole.Id);

            if (!mantemAdmin && await usuarios.CountAdminsAtivosAsync(ct) <= 1)
                return Result<bool>.Fail("O sistema deve ter ao menos um administrador ativo.");
        }

        await usuarios.SetRolesAsync(request.UsuarioId, roleIds, ct);
        await usuarios.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}
