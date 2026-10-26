namespace GatumsGourmet.Application.UseCases.V1.Usuarios;

public record UsuarioResponse(
    Guid Id,
    string Nome,
    string Email,
    string? Telefone,
    bool Ativo,
    IReadOnlyList<string> Roles,
    DateTime CriadoEm);
