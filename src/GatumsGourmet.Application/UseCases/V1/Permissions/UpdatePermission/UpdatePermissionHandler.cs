using FluentValidation;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Permissions.UpdatePermission;

public record UpdatePermissionRequest(int Id, string? Descricao, string Recurso, string Acao);

public class UpdatePermissionValidator : AbstractValidator<UpdatePermissionRequest>
{
    public UpdatePermissionValidator()
    {
        RuleFor(x => x.Recurso).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Acao).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}

public interface IUpdatePermissionHandler
{
    Task<Result<bool>> Handle(UpdatePermissionRequest request, CancellationToken ct = default);
}

public sealed class UpdatePermissionHandler(IPermissionRepository permissions) : IUpdatePermissionHandler
{
    public async Task<Result<bool>> Handle(UpdatePermissionRequest request, CancellationToken ct = default)
    {
        var permission = await permissions.GetByIdAsync(request.Id, ct);
        if (permission is null) return Result<bool>.Fail("Permissão não encontrada.");

        // O Codigo é a chave lógica usada em RequirePermission e não é alterado aqui.
        permission.Descricao = request.Descricao;
        permission.Recurso = request.Recurso;
        permission.Acao = request.Acao;
        permissions.Update(permission);
        await permissions.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}
