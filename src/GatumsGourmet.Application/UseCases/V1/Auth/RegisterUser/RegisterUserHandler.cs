using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Enums.Auth;

namespace GatumsGourmet.Application.UseCases.V1.Auth.RegisterUser;

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

