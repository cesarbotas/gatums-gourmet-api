namespace GatumsGourmet.Domain.Enums.Estoque;

/// <summary>
/// Tipo de movimento de estoque. Armazenado como string no Postgres
/// (mesma convenção dos outros enums do projeto).
/// </summary>
public enum TipoMovimentoEstoque
{
    /// <summary>Reposição de estoque (quantidade positiva).</summary>
    Entrada,
    /// <summary>Saída manual (quantidade negativa).</summary>
    Saida,
    /// <summary>Correção de inventário (positiva ou negativa).</summary>
    Ajuste,
    /// <summary>Débito automático no checkout (quantidade negativa).</summary>
    Venda
}
