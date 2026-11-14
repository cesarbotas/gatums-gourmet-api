namespace GatumsGourmet.Domain.Enums.Pagamentos;

/// <summary>
/// Formas de pagamento exclusivas do canal presencial. Inclui dinheiro (espécie),
/// que não existe no fluxo online. Armazenado como string no Postgres.
/// </summary>
public enum MetodoPagamentoPresencial
{
    /// <summary>Pagamento em espécie. Confirmação imediata — sem integração com PSP.</summary>
    Dinheiro,

    /// <summary>Cartão de débito via maquininha. Confirmação manual pelo operador.</summary>
    CartaoDebito,

    /// <summary>Cartão de crédito via maquininha. Confirmação manual pelo operador.</summary>
    CartaoCredito,

    /// <summary>Pix via QR Code dinâmico gerado pelo PSP. Confirmação via webhook ou polling.</summary>
    Pix
}
