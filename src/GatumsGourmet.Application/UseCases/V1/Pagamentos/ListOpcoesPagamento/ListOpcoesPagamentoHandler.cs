using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ListOpcoesPagamento;

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
