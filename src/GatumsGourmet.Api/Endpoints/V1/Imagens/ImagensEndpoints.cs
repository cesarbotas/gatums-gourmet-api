using GatumsGourmet.Api.Authorization;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Imagens.UploadImagem;

namespace GatumsGourmet.Api.Endpoints.V1.Imagens;

public static class ImagensEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            [AsParameters] ImagensRequest request,
            IUploadImagemHandler handler,
            CancellationToken ct) =>
        {
            // Guarda de borda do multipart: o campo "arquivo" pode nem vir na
            // requisição. A validação rica (vazio/tamanho/tipo) é do use case;
            // aqui só evitamos o NRE de abrir um IFormFile ausente. Usamos
            // Results.Problem para manter o mesmo shape (ProblemDetails) dos
            // demais erros 400 do projeto (ver ResultExtensions.ToHttpResult).
            if (request.Arquivo is null || request.Arquivo.Length <= 0)
            {
                return Results.Problem(
                    "Arquivo é obrigatório.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            await using var stream = request.Arquivo.OpenReadStream();

            var result = await handler.Handle(
                stream,
                request.Arquivo.ContentType,
                request.Arquivo.FileName,
                request.Arquivo.Length,
                ct);

            return result.ToHttpResult(StatusCodes.Status201Created);
        })
            .RequirePermission("produtos:escrever")
            .DisableAntiforgery();
    }
}
