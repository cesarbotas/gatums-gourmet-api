using GatumsGourmet.Domain.Enums.Produtos;

namespace GatumsGourmet.Domain.Entities.Produtos;

public class Produto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = default!;
    public string Descricao { get; set; } = default!;
    public string Categoria { get; set; } = default!;
    public decimal Preco { get; set; }
    public string FotoUrl { get; set; } = default!;
    public bool Disponivel { get; set; }
    public TipoProduto Tipo { get; set; }
    public DateTime CriadoEm { get; set; }
    public ICollection<ProdutoComboItem> ComboItens { get; set; } = [];
}

public class ProdutoComboItem
{
    public Guid Id { get; set; }
    public Guid ComboProdutoId { get; set; }
    public Guid ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public Produto Produto { get; set; } = default!;
}
