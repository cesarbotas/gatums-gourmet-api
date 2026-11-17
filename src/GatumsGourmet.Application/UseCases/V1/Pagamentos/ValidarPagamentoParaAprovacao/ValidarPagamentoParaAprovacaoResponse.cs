using GatumsGourmet.Domain.Enums.Pagamentos;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ValidarPagamentoParaAprovacao;

public record ValidarPagamentoParaAprovacaoResponse(
    Guid Id,
    Guid PedidoId,
    MetodoPagamento Metodo,
    decimal Valor,
    StatusTransacao Status,
    string? PixCopiaCola,
    DateTime? PixExpiraEm,
    string? CartaoUltimos4,
    string? CartaoBandeira,
    string? MensagemErro,
    DateTime CriadoEm,
    DateTime AtualizadoEm,
    string? PixQrCodeBase64 = null,
    string? Operadora = null,
    string? OperadoraRef = null,
    decimal? ValorRecebido = null,
    decimal? Troco = null
);
