using GatumsGourmet.Domain.Entities.Pagamentos;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos;

public record OpcaoPagamentoResponse(string Metodo, string Label, bool Disponivel);

public record TransacaoResponse(
    Guid Id,
    Guid PedidoId,
    string Metodo,
    decimal Valor,
    string Status,
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
    decimal? Troco = null);

public static class TransacaoMapper
{
    public static TransacaoResponse Map(TransacaoPagamento t) => new(
        t.Id,
        t.PedidoId,
        t.Metodo.ToString().ToLower(),
        t.Valor,
        t.Status.ToString().ToLower(),
        t.PixCopiaCola,
        t.PixExpiraEm,
        t.CartaoUltimos4,
        t.CartaoBandeira,
        t.MensagemErro,
        t.CriadoEm,
        t.AtualizadoEm,
        t.PixQrCodeBase64,
        t.Operadora,
        t.OperadoraRef,
        t.ValorRecebido,
        t.Troco);
}
