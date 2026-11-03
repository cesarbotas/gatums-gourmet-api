using FluentValidation;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Roles.CreateRole;

public record CreateRoleRequest(string Nome, string? Descricao);
public record CreateRoleResponse(int Id);

public class CreateRoleValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}

public interface ICreateRoleHandler
{
    Task<Result<CreateRoleResponse>> Handle(CreateRoleRequest request, CancellationToken ct = default);
}

public sealed class CreateRoleHandler(IRoleRepository roles) : ICreateRoleHandler
{
    public async Task<Result<CreateRoleResponse>> Handle(CreateRoleRequest request, CancellationToken ct = default)
    {
        if (await roles.GetByNomeAsync(request.Nome, ct) is not null)
            return Result<CreateRoleResponse>.Fail("Já existe uma role com este nome.");

        var role = new Role
        {
            Nome = request.Nome,
            Descricao = request.Descricao,
            Sistema = false
        };

        await roles.AddAsync(role, ct);
        await roles.SaveChangesAsync(ct);
        return Result<CreateRoleResponse>.Ok(new CreateRoleResponse(role.Id));
    }
}
