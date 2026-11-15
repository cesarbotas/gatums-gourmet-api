using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Estoque.ListMovimentos;

public record MovimentoResponse(
    Guid Id,
    string Tipo,
    int Quantidade,
    int SaldoResultante,
    Guid? PedidoId,
    string? Observacao,
    DateTime Data);

public interface IListMovimentosHandler
{
    Task<Result<IReadOnlyList<MovimentoResponse>>> Handle(Guid produtoId, CancellationToken ct = default);
}

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
