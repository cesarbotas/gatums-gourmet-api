using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Estoque.AdjustEstoque;

public interface IAdjustEstoqueHandler
{
    Task<Result<AdjustEstoqueResponse>> Handle(Guid produtoId, AdjustEstoqueRequest request, CancellationToken ct = default);
}
