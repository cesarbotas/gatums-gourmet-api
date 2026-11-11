using GatumsGourmet.Domain.Enums.Estoque;

namespace GatumsGourmet.Domain.Entities.Estoque;

/// <summary>
/// Registro de auditoria de toda alteração de estoque de um produto.
/// Cada movimento guarda o saldo resultante após a alteração, permitindo
/// reconstruir o histórico completo.
/// </summary>
public class MovimentoEstoque
{
    public Guid Id { get; set; }
    public Guid ProdutoId { get; set; }
    public TipoMovimentoEstoque Tipo { get; set; }
    /// <summary>Quantidade do movimento: positiva para Entrada, negativa para Saida/Venda.</summary>
    public int Quantidade { get; set; }
    /// <summary>Saldo do produto imediatamente após este movimento.</summary>
    public int SaldoResultante { get; set; }
    /// <summary>Pedido que originou o movimento (apenas quando Tipo = Venda).</summary>
    public Guid? PedidoId { get; set; }
    /// <summary>Usuário que fez o ajuste (apenas Entrada/Saida/Ajuste).</summary>
    public Guid? UsuarioId { get; set; }
    public string? Observacao { get; set; }
    public DateTime Data { get; set; }
}
