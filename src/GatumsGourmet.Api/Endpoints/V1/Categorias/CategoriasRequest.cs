using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Categorias;

/// <summary>
/// Parâmetros de query da listagem de categorias (<c>GET /categorias</c>),
/// agrupados numa classe só e ligados via <c>[AsParameters]</c> — mesmo padrão
/// de <see cref="Produtos.ProdutosRequest"/>.
/// </summary>
public class CategoriasRequest
{
    // int? de propósito: com [AsParameters], um int não-anulável vira query param
    // OBRIGATÓRIO no Minimal API (400 se o cliente chamar sem ?pagina=). Null cai
    // no default aplicado no endpoint.
    [FromQuery(Name = "pagina")]
    public int? Pagina { get; set; }

    [FromQuery(Name = "tamanho")]
    public int? Tamanho { get; set; }

    /// <summary><c>true</c> traz só as categorias ativas (catálogo).</summary>
    [FromQuery(Name = "apenasAtivas")]
    public bool? ApenasAtivas { get; set; }
}
