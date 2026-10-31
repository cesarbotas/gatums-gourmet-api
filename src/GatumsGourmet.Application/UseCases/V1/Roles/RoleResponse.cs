namespace GatumsGourmet.Application.UseCases.V1.Roles;

public record RoleResponse(int Id, string Nome, string? Descricao, bool Sistema, IReadOnlyList<string> Permissoes);
