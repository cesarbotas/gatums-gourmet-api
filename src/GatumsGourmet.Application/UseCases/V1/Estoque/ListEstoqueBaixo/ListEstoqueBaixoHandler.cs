using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Estoque.ListEstoqueBaixo;

public sealed class ListEstoqueBaixoHandler(IProdutoRepository produtos) : IListEstoqueBaixoHandler
{
    public async Task<Result<IReadOnlyList<EstoqueBaixoResponse>>>
        Handle(CancellationToken ct = default)
    {
        var items = await produtos.ListEstoqueBaixoAsync(ct);
        var dtos = items
            .Select(p => new EstoqueBaixoResponse(p.Id, p.Nome, p.Estoque, p.EstoqueMinimo))
            .ToList();
        return Result<IReadOnlyList<EstoqueBaixoResponse>>.Ok(dtos);
    }
}
