using FluentValidation;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Roles.UpdateRole;

public record UpdateRoleRequest(int Id, string Nome, string? Descricao);

public class UpdateRoleValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}

public interface IUpdateRoleHandler
{
    Task<Result<bool>> Handle(UpdateRoleRequest request, CancellationToken ct = default);
}

public sealed class UpdateRoleHandler(IRoleRepository roles) : IUpdateRoleHandler
{
    public async Task<Result<bool>> Handle(UpdateRoleRequest request, CancellationToken ct = default)
    {
        var role = await roles.GetByIdAsync(request.Id, ct);
        if (role is null) return Result<bool>.Fail("Role não encontrada.");
        if (role.Sistema) return Result<bool>.Fail("Roles de sistema não podem ser alteradas.");

        var existente = await roles.GetByNomeAsync(request.Nome, ct);
        if (existente is not null && existente.Id != role.Id)
            return Result<bool>.Fail("Já existe uma role com este nome.");

        role.Nome = request.Nome;
        role.Descricao = request.Descricao;
        roles.Update(role);
        await roles.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}
