using FluentValidation;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Permissions.CreatePermission;

public record CreatePermissionRequest(string Codigo, string? Descricao, string Recurso, string Acao);
public record CreatePermissionResponse(int Id);

public class CreatePermissionValidator : AbstractValidator<CreatePermissionRequest>
{
    public CreatePermissionValidator()
    {
        RuleFor(x => x.Codigo)
            .NotEmpty()
            .MaximumLength(100)
            .Must(c => c.Contains(':'))
            .WithMessage("Código deve seguir o formato recurso:acao.");
        RuleFor(x => x.Recurso).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Acao).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}

public interface ICreatePermissionHandler
{
    Task<Result<CreatePermissionResponse>> Handle(CreatePermissionRequest request, CancellationToken ct = default);
}

public sealed class CreatePermissionHandler(IPermissionRepository permissions) : ICreatePermissionHandler
{
    public async Task<Result<CreatePermissionResponse>> Handle(CreatePermissionRequest request, CancellationToken ct = default)
    {
        if (await permissions.GetByCodigoAsync(request.Codigo, ct) is not null)
            return Result<CreatePermissionResponse>.Fail("Já existe uma permissão com este código.");

        var permission = new Permission
        {
            Codigo = request.Codigo,
            Descricao = request.Descricao,
            Recurso = request.Recurso,
            Acao = request.Acao
        };

        await permissions.AddAsync(permission, ct);
        await permissions.SaveChangesAsync(ct);
        return Result<CreatePermissionResponse>.Ok(new CreatePermissionResponse(permission.Id));
    }
}
