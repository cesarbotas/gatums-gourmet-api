namespace GatumsGourmet.Domain.Entities.Auth;

public class UsuarioRole
{
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = default!;
    public int RoleId { get; set; }
    public Role Role { get; set; } = default!;
}
