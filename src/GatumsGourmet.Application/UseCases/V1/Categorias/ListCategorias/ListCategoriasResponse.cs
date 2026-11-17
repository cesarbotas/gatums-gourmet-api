namespace GatumsGourmet.Application.UseCases.V1.Categorias.ListCategorias;

public record ListCategoriasResponse(
    long Id,
    string Nome,
    string? Descricao,
    bool Ativo
);