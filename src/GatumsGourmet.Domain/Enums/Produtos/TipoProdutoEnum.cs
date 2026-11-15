namespace GatumsGourmet.Domain.Enums.Produtos;

/// <summary>
/// Tipo de produto no catálogo.
/// Armazenado como <see langword="string"/> no banco de dados.
/// </summary>
public enum TipoProdutoEnum
{
    /// <summary>1 — Produto individual, sem componentes filhos.</summary>
    Simples = 1,

    /// <summary>2 — Combinação de outros produtos simples (ex.: combo hambúrguer + batata + refri).</summary>
    Combo = 2
}
