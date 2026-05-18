using GatumsGourmet.Domain.Enums;

namespace GatumsGourmet.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Telefone { get; set; }
    public string SenhaHash { get; set; } = default!;
    public UserRole Role { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class Produto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = default!;
    public string Descricao { get; set; } = default!;
    public string Categoria { get; set; } = default!;
    public decimal Preco { get; set; }
    public string FotoUrl { get; set; } = default!;
    public bool Disponivel { get; set; }
    public TipoProduto Tipo { get; set; }
    public DateTime CriadoEm { get; set; }
    public ICollection<ProdutoComboItem> ComboItens { get; set; } = [];
}

public class ProdutoComboItem
{
    public Guid Id { get; set; }
    public Guid ComboProdutoId { get; set; }
    public Guid ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public Produto Produto { get; set; } = default!;
}

public class Cliente
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Telefone { get; set; }
    public string? Cidade { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class CarrinhoItem
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public string Nome { get; set; } = default!;
    public decimal Preco { get; set; }
    public string FotoUrl { get; set; } = default!;
}

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

public class TransacaoPagamento
{
    public Guid Id { get; set; }
    public Guid PedidoId { get; set; }
    public MetodoPagamento Metodo { get; set; }
    public decimal Valor { get; set; }
    public StatusTransacao Status { get; set; }
    public string Operadora { get; set; } = default!;
    public string? OperadoraRef { get; set; }
    public string? PixCopiaCola { get; set; }
    public DateTime? PixExpiraEm { get; set; }
    public string? CartaoUltimos4 { get; set; }
    public string? CartaoBandeira { get; set; }
    public string? MensagemErro { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}

public class Venda
{
    public Guid Id { get; set; }
    public Guid? PedidoId { get; set; }
    public Guid ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = default!;
    public int Quantidade { get; set; }
    public decimal Total { get; set; }
    public DateTime Data { get; set; }
}
