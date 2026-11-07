using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasResumo;

public interface IGetVendasResumoHandler
{
    Task<Result<VendasResumoResponse>> Handle(CancellationToken ct = default);
}