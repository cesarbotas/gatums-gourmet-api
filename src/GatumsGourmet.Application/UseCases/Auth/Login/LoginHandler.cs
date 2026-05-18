using FluentValidation;
using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Interfaces;

namespace GatumsGourmet.Application.UseCases.Auth.Login;

public record LoginRequest(string Email, string Senha);
public record LoginUserDto(Guid Id, string Nome, string Email, string Role);
public record LoginResponse(string Token, LoginUserDto User);

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Senha).NotEmpty();
    }
}

public interface ILoginHandler
{
    Task<Result<LoginResponse>> Handle(LoginRequest request, CancellationToken ct = default);
}

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
