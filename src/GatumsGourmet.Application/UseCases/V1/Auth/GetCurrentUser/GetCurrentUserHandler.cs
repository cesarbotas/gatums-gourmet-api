using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Auth.GetCurrentUser;

public sealed class GetCurrentUserHandler(IUsuarioRepository usuarios) : IGetCurrentUserHandler
{
    public async Task<Result<GetCurrentUserResponse>> Handle(Guid userId, CancellationToken ct = default)
    {
        var usuario = await usuarios.GetByIdComRolesAsync(userId, ct);
        if (usuario is null)
            return Result<GetCurrentUserResponse>.Fail("Usuário não encontrado.");

        var roles = usuario.UsuarioRoles.Select(ur => ur.Role.Nome).ToList();

        return Result<GetCurrentUserResponse>.Ok(
            new GetCurrentUserResponse(usuario.Id, usuario.Nome, usuario.Email, roles));
    }
}
