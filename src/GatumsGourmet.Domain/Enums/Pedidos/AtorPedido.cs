namespace GatumsGourmet.Domain.Enums.Pedidos;

/// <summary>
/// Identifica quem realizou uma ação no pedido (usado na timeline e nas regras do workflow).
/// Armazenado como <see langword="string"/> no banco de dados.
/// </summary>
public enum AtorPedido
{
    /// <summary>1 — Ação realizada pelo cliente (checkout, pagamento, cancelamento pré-aprovação).</summary>
    Cliente = 1,

    /// <summary>2 — Ação realizada por um usuário administrativo (aprovação, envio, cancelamento).</summary>
    Admin = 2
}
