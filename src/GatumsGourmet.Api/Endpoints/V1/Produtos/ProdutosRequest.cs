using GatumsGourmet.Domain.Enums.Produtos;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Produtos;

public class ProdutosRequest
{
    [FromQuery(Name = "pagina")]
    public int Pagina { get; set; } = 1;

    [FromQuery(Name = "tamanho")]
    public int Tamanho { get; set; } = 10;

    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "categoriaId")]
    public long? CategoriaId { get; set; }

    [FromQuery(Name = "tipo")]
    public TipoProduto? Tipo { get; set; }

    [FromQuery(Name = "disponivel")]
    public bool? Disponivel { get; set; }
}