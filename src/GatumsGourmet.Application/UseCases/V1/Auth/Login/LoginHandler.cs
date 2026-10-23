using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Auth.Login;

public sealed class LoginHandler(IUsuarioRepository usuarios, IPasswordHasher hasher, IJwtTokenGenerator jwt) : ILoginHandler
{
    public async Task<Result<LoginResponse>> Handle(LoginRequest request, CancellationToken ct = default)
    {
        var usuario = await usuarios.GetByEmailAsync(request.Email, ct);
        if (usuario is null || !hasher.Verify(request.Senha, usuario.SenhaHash))
            return Result<LoginResponse>.Fail("Credenciais inválidas.");

        var token = jwt.Generate(usuario.Id, usuario.Email, usuario.Role.ToString().ToLower());
        var dto = new LoginUserDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Role.ToString().ToLower());
        return Result<LoginResponse>.Ok(new LoginResponse(token, dto));
    }
}

