using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Estoque.ListEstoqueBaixo;

public interface IListEstoqueBaixoHandler
{
    Task<Result<IReadOnlyList<EstoqueBaixoResponse>>> Handle(CancellationToken ct = default);
}