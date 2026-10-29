using GatumsGourmet.Domain.Enums.Produtos;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Produtos;

public class ProdutosRequest
{
    // int? de propósito: com [AsParameters], um int não-anulável vira query param
    // OBRIGATÓRIO no Minimal API (400 se o cliente chamar sem ?pagina=). Null cai
    // no default aplicado no endpoint.
    [FromQuery(Name = "pagina")]
    public int? Pagina { get; set; }

    [FromQuery(Name = "tamanho")]
    public int? Tamanho { get; set; }

    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "categoriaId")]
    public long? CategoriaId { get; set; }

    [FromQuery(Name = "tipo")]
    public TipoProduto? Tipo { get; set; }

    [FromQuery(Name = "disponivel")]
    public bool? Disponivel { get; set; }
}