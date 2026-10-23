namespace GatumsGourmet.Domain.Entities.Produtos;

public class Categoria
{
    public long Id { get; set; }
    public string Nome { get; set; } = default!;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;
}
