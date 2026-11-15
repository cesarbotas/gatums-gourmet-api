namespace GatumsGourmet.Domain.Enums.Pagamentos;

/// <summary>
/// Status de uma transação de pagamento.
/// Armazenado como <see langword="string"/> no banco de dados.
/// </summary>
public enum StatusTransacaoEnum
{
    /// <summary>1 — Transação criada, aguardando processamento.</summary>
    Pendente = 1,

    /// <summary>2 — Pagamento confirmado pelo PSP ou pelo operador.</summary>
    Aprovado = 2,

    /// <summary>3 — Pagamento negado pelo PSP, operador ou pela regra de negócio (ex.: cancelamento).</summary>
    Recusado = 3,

    /// <summary>4 — QR Code Pix expirou sem pagamento.</summary>
    Expirado = 4
}
