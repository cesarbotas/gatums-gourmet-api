using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasResumo;

public record TopProdutoResponse(Guid ProdutoId, string Nome, int Quantidade, decimal Total);
public record VendaDiariaResponse(string Data, decimal Faturamento, int Pedidos);
public record VendasResumoResponse(decimal Faturamento, int TotalPedidos, decimal TicketMedio, IReadOnlyList<TopProdutoResponse> TopProdutos, IReadOnlyList<VendaDiariaResponse> SerieDiaria);

public interface IGetVendasResumoHandler
{
    Task<Result<VendasResumoResponse>> Handle(CancellationToken ct = default);
}

public sealed class GetVendasResumoHandler(IVendaRepository vendas) : IGetVendasResumoHandler
{
    public async Task<Result<VendasResumoResponse>> Handle(CancellationToken ct = default)
    {
        var linhas = await vendas.GetUltimos30DiasAsync(ct);
        var faturamento = linhas.Sum(v => v.Total);
        var pedidosUnicos = linhas.Where(v => v.PedidoId.HasValue).Select(v => v.PedidoId!.Value).Distinct().Count();
        var ticket = pedidosUnicos > 0 ? faturamento / pedidosUnicos : 0;
        var top = linhas.GroupBy(v => new { v.ProdutoId, v.ProdutoNome })
            .Select(g => new TopProdutoResponse(g.Key.ProdutoId, g.Key.ProdutoNome, g.Sum(v => v.Quantidade), g.Sum(v => v.Total)))
            .OrderByDescending(x => x.Total).Take(5).ToList();
        var serie = linhas.GroupBy(v => v.Data.Date)
            .Select(g => new VendaDiariaResponse(g.Key.ToString("yyyy-MM-dd"), g.Sum(v => v.Total), g.Select(v => v.PedidoId).Distinct().Count()))
            .OrderBy(x => x.Data).ToList();
        return Result<VendasResumoResponse>.Ok(new VendasResumoResponse(faturamento, pedidosUnicos, ticket, top, serie));
    }
}

