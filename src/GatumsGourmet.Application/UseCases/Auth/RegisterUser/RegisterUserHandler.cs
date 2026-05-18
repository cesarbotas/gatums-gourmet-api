using FluentValidation;
using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Entities;
using GatumsGourmet.Domain.Enums;
using GatumsGourmet.Domain.Interfaces;

namespace GatumsGourmet.Application.UseCases.Auth.RegisterUser;

public record RegisterUserRequest(string Nome, string Email, string Senha, string? Telefone);
public record RegisterUserResponse(Guid Id, string Nome, string Email, string Role);

public class RegisterUserValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Senha).NotEmpty().MinimumLength(6);
    }
}

public interface IRegisterUserHandler
{
    Task<Result<RegisterUserResponse>> Handle(RegisterUserRequest request, CancellationToken ct = default);
}

public sealed class RegisterUserHandler(IUsuarioRepository usuarios, IPasswordHasher hasher, IClock clock) : IRegisterUserHandler
{
    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserRequest request, CancellationToken ct = default)
    {
        var existing = await usuarios.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            return Result<RegisterUserResponse>.Fail("E-mail já cadastrado.");

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Email = request.Email,
            Telefone = request.Telefone,
            SenhaHash = hasher.Hash(request.Senha),
            Role = UserRole.Cliente,
            CriadoEm = clock.UtcNow
        };

        await usuarios.AddAsync(usuario, ct);
        await usuarios.SaveChangesAsync(ct);

        return Result<RegisterUserResponse>.Ok(new RegisterUserResponse(usuario.Id, usuario.Nome, usuario.Email, "cliente"));
    }
}
