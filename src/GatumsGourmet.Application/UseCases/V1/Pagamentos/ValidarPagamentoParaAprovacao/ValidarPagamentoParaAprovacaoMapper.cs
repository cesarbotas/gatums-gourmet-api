using GatumsGourmet.Domain.Entities.Pagamentos;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ValidarPagamentoParaAprovacao;

public static class ValidarPagamentoParaAprovacaoMapper
{
    public static ValidarPagamentoParaAprovacaoResponse Map(TransacaoPagamento t) => new(
        t.Id,
        t.PedidoId,
        t.Metodo,
        t.Valor,
        t.Status,
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
