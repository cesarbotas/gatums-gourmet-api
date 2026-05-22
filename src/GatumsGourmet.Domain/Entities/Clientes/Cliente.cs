namespace GatumsGourmet.Domain.Entities.Clientes;

public class Cliente
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Telefone { get; set; }
    public Endereco Endereco { get; set; } = default!;
    public DateTime CriadoEm { get; set; }
}
