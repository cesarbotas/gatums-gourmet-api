namespace GatumsGourmet.Domain.Enums.Pedidos;

/// <summary>
/// Canal pelo qual o pedido foi originado. Armazenado como string no Postgres
/// (mesma convenção dos demais enums do projeto).
/// </summary>
public enum CanalVenda
{
    /// <summary>Venda pelo site/app do cliente (fluxo padrão: carrinho → checkout).</summary>
    Online,

    /// <summary>Venda presencial via tela /admin/venda-rapida (operador no balcão).</summary>
    Presencial
}
