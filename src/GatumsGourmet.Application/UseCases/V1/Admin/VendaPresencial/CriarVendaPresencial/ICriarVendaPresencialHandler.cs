using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.CriarVendaPresencial;

public interface ICriarVendaPresencialHandler
{
    Task<Result<CriarVendaPresencialResponse>> Handle(
        CriarVendaPresencialRequest request,
        CancellationToken ct = default);
}
