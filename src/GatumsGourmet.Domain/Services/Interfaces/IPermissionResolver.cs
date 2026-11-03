namespace GatumsGourmet.Domain.Services.Interfaces;

public interface IPermissionResolver
{
    Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissoes)> ResolveAsync(Guid usuarioId, CancellationToken ct = default);
}
