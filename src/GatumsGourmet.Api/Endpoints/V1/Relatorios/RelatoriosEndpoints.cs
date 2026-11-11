using GatumsGourmet.Api.Authorization;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasResumo;
using GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasPorPeriodo;

namespace GatumsGourmet.Api.Endpoints.V1.Relatorios;

public static class RelatoriosEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/vendas/resumo", async (IGetVendasResumoHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttpResult())
            .RequirePermission("relatorios:ler");
    }

    public static void MapRelatorioPorPeriodo(RouteGroupBuilder group)
    {
        group.MapGet("/vendas/periodo", async (
            [AsParameters] RelatorioVendaPorPeriodoRequest request,
            IGetVendasPorPeriodoHandler handler,
            CancellationToken ct) =>
            {
                var vendasRequest = GetRelatorioVendaPorPeriodoRequest.Criar(request.DataInicio, request.DataFim);
                var resultado = await handler.Handle(vendasRequest, ct);
                return resultado.ToHttpResult();
            })
            .WithDescription("Retorna o resumo das vendas por período")
            .RequirePermission("relatorios:ler");
    }
}
