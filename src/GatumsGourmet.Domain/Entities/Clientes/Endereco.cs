namespace GatumsGourmet.Domain.Entities.Clientes;

public class Endereco
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string Logradouro { get; set; } = default!;
    public string Numero { get; set; } = default!;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = default!;
    public string Cidade { get; set; } = default!;
    public string Estado { get; set; } = default!;
    public string Cep { get; set; } = default!;
}