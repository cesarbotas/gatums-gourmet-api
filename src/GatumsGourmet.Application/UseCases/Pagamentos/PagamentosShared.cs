using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Entities;

namespace GatumsGourmet.Application.UseCases.Pagamentos;

public record OpcaoPagamentoResponse(string Metodo, string Label, bool Disponivel);

public record TransacaoResponse(
    Guid Id, Guid PedidoId, string Metodo, decimal Valor, string Status,
    string? PixCopiaCola, DateTime? PixExpiraEm,
    string? CartaoUltimos4, string? CartaoBandeira,
    string? MensagemErro, DateTime CriadoEm, DateTime AtualizadoEm);

public static class TransacaoMapper
{
    public static TransacaoResponse Map(TransacaoPagamento t) => new(
        t.Id, t.PedidoId, t.Metodo.ToString().ToLower(), t.Valor, t.Status.ToString().ToLower(),
        t.PixCopiaCola, t.PixExpiraEm,
        t.CartaoUltimos4, t.CartaoBandeira,
        t.MensagemErro, t.CriadoEm, t.AtualizadoEm);
}

public interface IListOpcoesPagamentoHandler
{
    Task<Result<IReadOnlyList<OpcaoPagamentoResponse>>> Handle(CancellationToken ct = default);
}

public sealed class ListOpcoesPagamentoHandler : IListOpcoesPagamentoHandler
{
    private static readonly IReadOnlyList<OpcaoPagamentoResponse> _opcoes =
    [
        new("cartao_credito", "Cartão de Crédito", true),
        new("cartao_debito", "Cartão de Débito", true),
        new("pix", "Pix", true),
    ];

    public Task<Result<IReadOnlyList<OpcaoPagamentoResponse>>> Handle(CancellationToken ct = default) =>
        Task.FromResult(Result<IReadOnlyList<OpcaoPagamentoResponse>>.Ok(_opcoes));
}
