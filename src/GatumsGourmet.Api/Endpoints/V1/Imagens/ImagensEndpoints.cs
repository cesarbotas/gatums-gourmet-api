using GatumsGourmet.Api.Authorization;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Imagens.UploadImagem;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Imagens;

public static class ImagensEndpoints
{
    // Teto de corpo alto (50 MB): confiamos na validação do handler (5 MB)
    // para retornar 400 com mensagem amigável. O limite do framework fica
    // alto apenas para evitar que Kestrel rejeite a conexão com 413/500
    // antes do handler validar — assim o handler sempre tem chance de validar
    // e retornar o 400 com a mensagem de negócio.
    private const long LimiteCorpoBytes = 50L * 1024 * 1024;

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
            .DisableAntiforgery()
            // Limita o tamanho do corpo e do multipart no limite do framework.
            .WithMetadata(
                new RequestSizeLimitAttribute(LimiteCorpoBytes),
                new RequestFormLimitsAttribute { MultipartBodyLengthLimit = LimiteCorpoBytes });
    }
}
