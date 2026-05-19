namespace GatumsGourmet.Domain.Entities.Vendas;

public class Venda
{
    public Guid Id { get; set; }
    public Guid? PedidoId { get; set; }
    public Guid ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = default!;
    public int Quantidade { get; set; }
    public decimal Total { get; set; }
    public DateTime Data { get; set; }
}
