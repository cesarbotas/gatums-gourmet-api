using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Estoque.ListMovimentos;

public interface IListMovimentosHandler
{
    Task<Result<IReadOnlyList<MovimentoResponse>>> Handle(Guid produtoId, CancellationToken ct = default);
}