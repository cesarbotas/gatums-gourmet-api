namespace GatumsGourmet.Application.UseCases.V1.Permissions;

public record PermissionResponse(int Id, string Codigo, string? Descricao, string Recurso, string Acao);
