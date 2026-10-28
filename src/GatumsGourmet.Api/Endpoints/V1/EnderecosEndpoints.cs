using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Enderecos.BuscarEnderecoPorCep;

namespace GatumsGourmet.Api.Endpoints.V1;

public static class EnderecosEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        // Consulta pública de endereço por CEP (usada no autopreenchimento do
        // formulário de endereço). Read-only; não grava nada.
        group.MapGet("/cep/{cep}", async (string cep, IBuscarEnderecoPorCepHandler handler, CancellationToken ct) =>
            (await handler.Handle(cep, ct)).ToHttpResult());
    }
}
