namespace GatumsGourmet.Domain.Entities.Carrinho;

public class CarrinhoItem
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public string Nome { get; set; } = default!;
    public decimal Preco { get; set; }
    public string FotoUrl { get; set; } = default!;
}
