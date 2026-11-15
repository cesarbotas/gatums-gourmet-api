namespace GatumsGourmet.Domain.Enums.Pagamentos;

/// <summary>
/// Forma de pagamento usada no fluxo online (site/app).
/// Os valores inteiros são enviados no payload JSON do endpoint de pagamento.
/// Armazenado como <see langword="string"/> no banco de dados.
/// </summary>
public enum MetodoPagamentoEnum
{
    /// <summary>1 — Cartão de débito (processado pela maquininha ou gateway).</summary>
    CartaoDebito = 1,

    /// <summary>2 — Cartão de crédito (processado pela maquininha ou gateway).</summary>
    CartaoCredito = 2,

    /// <summary>3 — Pix via QR Code dinâmico (Mercado Pago Orders API).</summary>
    Pix = 3
}
