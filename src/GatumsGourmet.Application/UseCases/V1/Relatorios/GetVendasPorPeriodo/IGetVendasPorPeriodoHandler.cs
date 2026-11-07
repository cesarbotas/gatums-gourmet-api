using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasPorPeriodo;

public interface IGetVendasPorPeriodoHandler
{
    Task<Result<VendasPorPeriodoResponse>> Handle(
        GetRelatorioVendaPorPeriodoRequest request,
        CancellationToken ct);
}
