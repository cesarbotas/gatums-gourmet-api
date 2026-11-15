namespace GatumsGourmet.Domain.Enums.Pedidos;

public enum PedidoEtapa
{
    Criado,
    Aprovado,
    Pagamento,
    PagamentoAprovado,
    EnvioPendente,
    Entregue,
    Finalizado,
    /// <summary>Pedido cancelado. Estado terminal — sem transições a partir daqui.</summary>
    Cancelado
}
