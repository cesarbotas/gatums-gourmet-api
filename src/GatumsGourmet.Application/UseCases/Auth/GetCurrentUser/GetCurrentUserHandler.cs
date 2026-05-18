using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;

namespace GatumsGourmet.Application.UseCases.Auth.GetCurrentUser;

public record GetCurrentUserResponse(Guid Id, string Nome, string Email, string Role);

public interface IGetCurrentUserHandler
{
    Task<Result<GetCurrentUserResponse>> Handle(Guid userId, CancellationToken ct = default);
}

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
