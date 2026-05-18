using GatumsGourmet.Domain.Enums.Pagamentos;

namespace GatumsGourmet.Domain.Entities.Pagamentos;

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
