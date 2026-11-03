namespace GatumsGourmet.Domain.Entities.Auth;

public class Usuario
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Telefone { get; set; }
    public string SenhaHash { get; set; } = default!;
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public ICollection<UsuarioRole> UsuarioRoles { get; set; } = [];

    public Usuario() { }
}
