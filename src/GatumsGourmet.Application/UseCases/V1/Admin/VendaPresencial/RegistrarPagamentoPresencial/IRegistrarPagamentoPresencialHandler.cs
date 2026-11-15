using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Application.UseCases.V1.Pagamentos;

namespace GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.RegistrarPagamentoPresencial;

public interface IRegistrarPagamentoPresencialHandler
{
    Task<Result<TransacaoResponse>> Handle(
        RegistrarPagamentoPresencialRequest request,
        CancellationToken ct = default);
}
