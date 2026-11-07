namespace GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasResumo;

public record TopProdutoResponse(
    Guid ProdutoId,
    string Nome,
    int Quantidade,
    decimal Total
);
