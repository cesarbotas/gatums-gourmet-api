using GatumsGourmet.Domain.Enums.Produtos;

namespace GatumsGourmet.Application.UseCases.Produtos.CreateProduto;

public record ComboItemInput(Guid ProdutoId, int Quantidade);
public record CreateProdutoRequest(string Nome, string Descricao, long CategoriaId, decimal Preco, string FotoUrl, bool Disponivel, TipoProduto Tipo, IReadOnlyList<ComboItemInput>? Itens);
