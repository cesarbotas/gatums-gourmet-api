namespace GatumsGourmet.Application.UseCases.V1.Categorias.ListCategorias;

public record ListCategoriasRequest(
    int Pagina = 1,
    int Tamanho = 10,
    bool? ApenasAtivas = null
);