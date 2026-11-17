using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ListOpcoesPagamento;

public interface IListOpcoesPagamentoHandler
{
    Task<Result<IReadOnlyList<OpcaoPagamentoResponse>>> Handle(CancellationToken ct = default);
}
