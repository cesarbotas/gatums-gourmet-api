using GatumsGourmet.Api.Authorization;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasResumo;

namespace GatumsGourmet.Api.Endpoints.V1;

public static class RelatoriosEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/vendas/resumo", async (IGetVendasResumoHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttpResult())
            .RequirePermission("relatorios:ler");
    }
}
