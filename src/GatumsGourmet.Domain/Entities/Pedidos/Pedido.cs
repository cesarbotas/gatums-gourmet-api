using GatumsGourmet.Domain.Enums.Pedidos;

namespace GatumsGourmet.Domain.Entities.Pedidos;

public class Pedido
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string ClienteNome { get; set; } = default!;
    public decimal Total { get; set; }
    public DateTime Data { get; set; }
    public PedidoEtapa StatusAtual { get; set; }
    public ICollection<PedidoItem> Itens { get; set; } = [];
    public ICollection<PedidoTimelineEvento> Timeline { get; set; } = [];
    public PedidoPagamento? Pagamento { get; set; }
}

public class PedidoItem
{
    public Guid Id { get; set; }
    public Guid PedidoId { get; set; }
    public Guid ProdutoId { get; set; }
    public string Nome { get; set; } = default!;
    public decimal Preco { get; set; }
    public string FotoUrl { get; set; } = default!;
    public int Quantidade { get; set; }
}

public class PedidoTimelineEvento
{
    public Guid Id { get; set; }
    public Guid PedidoId { get; set; }
    public PedidoEtapa Etapa { get; set; }
    public DateTime Em { get; set; }
    public PedidoActor Por { get; set; }
    public string? Observacao { get; set; }
    public EntregaTipo? EntregaTipo { get; set; }
}

public class PedidoPagamento
{
    public Guid PedidoId { get; set; }
    public Guid TransacaoId { get; set; }
    public string Resumo { get; set; } = default!;
    public DateTime AprovadoEm { get; set; }
}
