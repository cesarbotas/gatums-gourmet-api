namespace GatumsGourmet.Domain.Enums.Pedidos;

/// <summary>
/// Status (etapa) de um pedido ao longo do seu ciclo de vida.
/// Os valores inteiros são usados em payloads JSON (ex.: ao avançar etapa via API).
/// Armazenado como <see langword="string"/> no banco de dados.
/// </summary>
public enum StatusPedido
{
    /// <summary>1 — Pedido criado pelo cliente, aguardando aprovação do admin.</summary>
    Criado = 1,

    /// <summary>2 — Admin aprovou o pedido; aguardando pagamento pelo cliente.</summary>
    Aprovado = 2,

    /// <summary>3 — Cliente iniciou o pagamento; aguardando confirmação.</summary>
    Pagamento = 3,

    /// <summary>4 — Pagamento aprovado pelo admin; pedido pronto para envio.</summary>
    PagamentoAprovado = 4,

    /// <summary>5 — Em preparação/triagem para envio.</summary>
    EnvioPendente = 5,

    /// <summary>6 — Pedido entregue ao cliente.</summary>
    Entregue = 6,

    /// <summary>7 — Fluxo concluído com sucesso.</summary>
    Finalizado = 7,

    /// <summary>8 — Pedido cancelado. Estado terminal.</summary>
    Cancelado = 8
}
