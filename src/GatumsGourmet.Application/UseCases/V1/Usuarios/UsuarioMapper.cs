using GatumsGourmet.Domain.Entities.Auth;

namespace GatumsGourmet.Application.UseCases.V1.Usuarios;

internal static class UsuarioMapper
{
    public static UsuarioResponse ToResponse(Usuario usuario) => new(
        usuario.Id,
        usuario.Nome,
        usuario.Email,
        usuario.Telefone,
        usuario.Ativo,
        usuario.UsuarioRoles.Select(ur => ur.Role.Nome).OrderBy(n => n).ToList(),
        usuario.CriadoEm);

    /// <summary>Nome da role de administrador do sistema.</summary>
    public const string AdminRoleNome = "Admin";

    public static bool TemRoleAdmin(Usuario usuario) =>
        usuario.UsuarioRoles.Any(ur => ur.Role.Nome == AdminRoleNome);
}
