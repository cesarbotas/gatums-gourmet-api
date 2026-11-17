namespace GatumsGourmet.Application.UseCases.V1.Carrinho.GetCarrinho;

public record CarrinhoResponse(
    IReadOnlyList<CarrinhoItemResponse> Itens,
    decimal Total
);

public record CarrinhoItemResponse(
    Guid ProdutoId,
    string Nome,
    decimal Preco,
    string FotoUrl,
    int Quantidade);
