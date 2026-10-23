using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Auth.GetCurrentUser;

public sealed class GetCurrentUserHandler(IUsuarioRepository usuarios) : IGetCurrentUserHandler
{
    public async Task<Result<GetCurrentUserResponse>> Handle(Guid userId, CancellationToken ct = default)
    {
        var usuario = await usuarios.GetByIdAsync(userId, ct);
        if (usuario is null)
            return Result<GetCurrentUserResponse>.Fail("Usuário não encontrado.");

        return Result<GetCurrentUserResponse>.Ok(
            new GetCurrentUserResponse(usuario.Id, usuario.Nome, usuario.Email, usuario.Role.ToString().ToLower()));
    }
}

