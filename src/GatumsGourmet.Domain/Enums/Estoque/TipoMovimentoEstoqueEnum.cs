namespace GatumsGourmet.Domain.Enums.Estoque;

/// <summary>
/// Tipo de movimento que alterou o estoque de um produto.
/// Armazenado como <see langword="string"/> no banco de dados.
/// </summary>
public enum TipoMovimentoEstoqueEnum
{
    /// <summary>1 — Reposição de estoque (quantidade positiva).</summary>
    Entrada = 1,

    /// <summary>2 — Saída manual (quantidade negativa, ex.: descarte, brinde).</summary>
    Saida = 2,

    /// <summary>3 — Correção de inventário (pode ser positiva ou negativa).</summary>
    Ajuste = 3,

    /// <summary>4 — Débito automático gerado pelo checkout de um pedido.</summary>
    Venda = 4
}
