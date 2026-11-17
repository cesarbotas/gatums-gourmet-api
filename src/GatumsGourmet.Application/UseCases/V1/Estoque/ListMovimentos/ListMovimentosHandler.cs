using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Estoque.ListMovimentos;

public sealed class ListMovimentosHandler(IMovimentoEstoqueRepository movimentos)
    : IListMovimentosHandler
{
    public async Task<Result<IReadOnlyList<MovimentoResponse>>>
        Handle(Guid produtoId, CancellationToken ct = default)
    {
        var items = await movimentos.ListByProdutoAsync(produtoId, ct);
        var dtos = items
            .Select(m => new MovimentoResponse(
                m.Id,
                m.Tipo.ToString().ToLower(),
                m.Quantidade,
                m.SaldoResultante,
                m.PedidoId,
                m.Observacao,
                m.Data))
            .ToList();
        return Result<IReadOnlyList<MovimentoResponse>>.Ok(dtos);
    }
}
