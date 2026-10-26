using FluentValidation;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Usuarios.CreateUsuario;

public record CreateUsuarioRequest(
    string Nome,
    string Email,
    string Senha,
    string? Telefone,
    IReadOnlyList<int> RoleIds);

public record CreateUsuarioResponse(Guid Id);

public class CreateUsuarioValidator : AbstractValidator<CreateUsuarioRequest>
{
    public CreateUsuarioValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Senha).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Telefone).MaximumLength(30);
    }
}

public interface ICreateUsuarioHandler
{
    Task<Result<CreateUsuarioResponse>> Handle(CreateUsuarioRequest request, CancellationToken ct = default);
}

public sealed class CreateUsuarioHandler(
    IUsuarioRepository usuarios,
    IRoleRepository roles,
    IPasswordHasher hasher,
    IClock clock) : ICreateUsuarioHandler
{
    public async Task<Result<CreateUsuarioResponse>> Handle(CreateUsuarioRequest request, CancellationToken ct = default)
    {
        if (await usuarios.GetByEmailAsync(request.Email, ct) is not null)
            return Result<CreateUsuarioResponse>.Fail("E-mail já cadastrado.");

        var roleIds = (request.RoleIds ?? []).Distinct().ToList();

        // Validação amigável: todos os RoleIds devem existir antes de persistir,
        // evitando um erro de FK opaco vindo do banco.
        foreach (var roleId in roleIds)
        {
            if (await roles.GetByIdAsync(roleId, ct) is null)
                return Result<CreateUsuarioResponse>.Fail($"Role {roleId} não encontrada.");
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Email = request.Email,
            Telefone = request.Telefone,
            SenhaHash = hasher.Hash(request.Senha),
            Ativo = true,
            CriadoEm = clock.UtcNow
        };

        // Persistência atômica: adiciona o usuário e vincula as roles antes de
        // um único SaveChanges, para que ambos sejam gravados na mesma transação.
        // Evita o estado inconsistente de usuário criado sem roles caso o
        // vínculo falhasse num segundo SaveChanges.
        await usuarios.AddAsync(usuario, ct);

        if (roleIds.Count > 0)
            await usuarios.SetRolesAsync(usuario.Id, roleIds, ct);

        await usuarios.SaveChangesAsync(ct);

        return Result<CreateUsuarioResponse>.Ok(new CreateUsuarioResponse(usuario.Id));
    }
}
