using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.Relatorios.GetVendasResumo;

namespace GatumsGourmet.Api.Endpoints;

public static class RelatoriosEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/vendas/resumo", async (IGetVendasResumoHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttpResult())
            .RequireAuthorization("AdminOnly");
    }
}
