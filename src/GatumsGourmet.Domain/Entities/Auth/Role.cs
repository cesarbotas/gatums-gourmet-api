namespace GatumsGourmet.Domain.Entities.Auth;

public class Role
{
    public int Id { get; set; }
    public string Nome { get; set; } = default!;
    public string? Descricao { get; set; }
    public bool Sistema { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<UsuarioRole> UsuarioRoles { get; set; } = [];
}
