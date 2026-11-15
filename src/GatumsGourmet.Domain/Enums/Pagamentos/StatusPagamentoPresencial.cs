namespace GatumsGourmet.Domain.Enums.Pagamentos;

/// <summary>
/// Status de uma transação de pagamento presencial. Mais granular que
/// <see cref="StatusTransacao"/> para suportar os estados intermediários
/// da maquininha e do Pix presencial. Armazenado como string no Postgres.
/// </summary>
public enum StatusPagamentoPresencial
{
    /// <summary>Aguardando confirmação manual do operador (cartão na maquininha).</summary>
    AguardandoConfirmacaoOperador,

    /// <summary>Aguardando pagamento Pix pelo cliente (QR Code exibido).</summary>
    AguardandoPagamento,

    /// <summary>Pagamento aprovado (dinheiro, cartão confirmado pelo operador, ou Pix aprovado).</summary>
    Aprovado,

    /// <summary>Pagamento recusado (cartão negado pelo operador ou pela maquininha).</summary>
    Recusado,

    /// <summary>QR Code Pix expirado sem pagamento.</summary>
    Expirado
}
