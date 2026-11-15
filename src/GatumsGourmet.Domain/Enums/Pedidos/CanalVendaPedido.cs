namespace GatumsGourmet.Domain.Enums.Pedidos;

/// <summary>
/// Canal pelo qual o pedido foi originado.
/// Armazenado como <see langword="string"/> no banco de dados.
/// </summary>
public enum CanalVendaPedido
{
    /// <summary>1 — Venda pelo site ou aplicativo (checkout do cliente).</summary>
    Online = 1,

    /// <summary>2 — Venda no balcão via tela <c>/admin/venda-rapida</c>.</summary>
    Presencial = 2
}
