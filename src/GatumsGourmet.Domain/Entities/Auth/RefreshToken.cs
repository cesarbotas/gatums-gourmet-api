namespace GatumsGourmet.Domain.Entities.Auth;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string TokenHash { get; set; } = default!;
    public Guid FamiliaId { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime CriadoEm { get; set; }
    public bool Revogado { get; set; }
    public DateTime? RevogadoEm { get; set; }
    public string? SubstituidoPorTokenHash { get; set; }
    public string? MotivoRevogacao { get; set; }
    public string? CriadoPorIp { get; set; }
}
