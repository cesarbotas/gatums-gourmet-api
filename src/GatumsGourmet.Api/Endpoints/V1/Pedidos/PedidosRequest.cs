using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Pedidos;

/// <summary>
/// Parâmetros de query da listagem de pedidos (<c>GET /pedidos</c>), agrupados
/// numa classe só e ligados via <c>[AsParameters]</c> — mesmo padrão de
/// <see cref="Produtos.ProdutosRequest"/>. O <c>ClaimsPrincipal</c> e a lógica
/// de autorização continuam no endpoint (não são parâmetros de query).
/// </summary>
public class PedidosRequest
{
    // int? de propósito: com [AsParameters], um int não-anulável vira query param
    // OBRIGATÓRIO no Minimal API (400 se o cliente chamar sem ?pagina=). Null cai
    // no default aplicado no endpoint.
    [FromQuery(Name = "pagina")]
    public int? Pagina { get; set; }

    [FromQuery(Name = "tamanho")]
    public int? Tamanho { get; set; }
}
