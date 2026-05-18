namespace GatumsGourmet.Application.UseCases.Produtos.GetProdutoById;

public record ComboItemResponse(Guid ProdutoId, int Quantidade);
public record ProdutoDetailResponse(Guid Id, string Nome, string Descricao, string Categoria, decimal Preco, string FotoUrl, bool Disponivel, string Tipo, IReadOnlyList<ComboItemResponse> Itens);
