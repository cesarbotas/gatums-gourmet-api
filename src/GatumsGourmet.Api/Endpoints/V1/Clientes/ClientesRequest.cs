using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Clientes;

/// <summary>
/// Parâmetros de query da listagem de clientes (<c>GET /clientes</c>),
/// agrupados numa classe só e ligados via <c>[AsParameters]</c> — mesmo padrão
/// de <see cref="Produtos.ProdutosRequest"/>.
/// </summary>
public class ClientesRequest
{
    // int? de propósito: com [AsParameters], um int não-anulável vira query param
    // OBRIGATÓRIO no Minimal API (400 se o cliente chamar sem ?pagina=). Null cai
    // no default aplicado no endpoint.
    [FromQuery(Name = "pagina")]
    public int? Pagina { get; set; }

    [FromQuery(Name = "tamanho")]
    public int? Tamanho { get; set; }

    /// <summary>Busca textual por nome/email.</summary>
    [FromQuery(Name = "search")]
    public string? Search { get; set; }
}
