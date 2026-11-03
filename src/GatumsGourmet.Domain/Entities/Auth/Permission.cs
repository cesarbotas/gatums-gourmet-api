namespace GatumsGourmet.Domain.Entities.Auth;

public class Permission
{
    public int Id { get; set; }
    public string Codigo { get; set; } = default!;   // formato recurso:acao
    public string? Descricao { get; set; }
    public string Recurso { get; set; } = default!;
    public string Acao { get; set; } = default!;
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
