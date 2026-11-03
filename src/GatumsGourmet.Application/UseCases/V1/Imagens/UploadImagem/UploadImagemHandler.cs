using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V1.Imagens.UploadImagem;

/// <summary>
/// Valida o arquivo recebido (storage configurado + tamanho máximo + tipo real
/// por magic bytes) e, sendo válido, delega a gravação ao <see cref="IImageStorage"/>.
/// Registra o passo a passo no log para diagnóstico (sem expor segredos).
/// </summary>
public sealed class UploadImagemHandler(
    IImageStorage storage,
    IConfiguration config,
    ILogger<UploadImagemHandler> logger)
    : IUploadImagemHandler
{
    public async Task<Result<UploadImagemResponse>> Handle(
        Stream conteudo,
        string? contentType,
        string? nomeArquivo,
        long tamanho,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Upload de imagem iniciado: nome={Nome}, contentType={ContentType}, tamanho={Tamanho} bytes.",
            nomeArquivo, contentType, tamanho);

        // Guarda de configuração: se o storage não está configurado (Storage:ServiceUrl
        // vazio — ex.: a variável de ambiente Storage__ServiceUrl não chegou ao processo),
        // falha com mensagem clara (→ 400) ANTES de acionar o storage, em vez de deixar o
        // UnconfiguredImageStorage lançar uma exceção que vira 500 opaco.
        var serviceUrl = config["Storage:ServiceUrl"];
        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            logger.LogWarning(
                "Upload de imagem indisponível: Storage:ServiceUrl está vazio. " +
                "Verifique a variável de ambiente Storage__ServiceUrl no ambiente atual.");
            return Result<UploadImagemResponse>.Fail(
                "Upload de imagem indisponível: storage não configurado.");
        }

        var maxMb = config.GetValue<int?>("Storage:MaxFileSizeMB") ?? 5;

        if (tamanho <= 0)
        {
            logger.LogWarning("Upload rejeitado: arquivo vazio (tamanho={Tamanho}).", tamanho);
            return Result<UploadImagemResponse>.Fail("Arquivo vazio.");
        }

        if (tamanho > maxMb * 1024L * 1024L)
        {
            logger.LogWarning(
                "Upload rejeitado: arquivo de {Tamanho} bytes excede o limite de {MaxMb} MB.",
                tamanho, maxMb);
            return Result<UploadImagemResponse>.Fail(
                $"Arquivo excede o tamanho máximo de {maxMb} MB.");
        }

        // Copia o conteúdo para um MemoryStream uma única vez: assim conseguimos
        // detectar o tipo pelos primeiros bytes e, em seguida, rebobinar e
        // reenviar exatamente o mesmo conteúdo ao storage — mesmo que o stream de
        // entrada não seja seekable.
        using var ms = new MemoryStream();
        await conteudo.CopyToAsync(ms, ct);
        ms.Position = 0;

        var buffer = new byte[12];
        var lidos = await ms.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);

        var (ok, extensao, ctype) = ImageTypeDetector.Detectar(buffer.AsSpan(0, lidos));
        if (!ok)
        {
            logger.LogWarning(
                "Upload rejeitado: tipo não suportado (magic bytes não batem com JPEG/PNG/WebP).");
            return Result<UploadImagemResponse>.Fail(
                "Tipo de imagem não suportado. Use JPEG, PNG ou WebP.");
        }

        ms.Position = 0;
        logger.LogInformation(
            "Upload validado (tipo={Extensao}); enviando ao storage sob o prefixo 'produtos'.",
            extensao);

        var resultado = await storage.UploadAsync(ms, ctype, extensao, "produtos", ct);

        logger.LogInformation("Upload concluído: objectKey={ObjectKey}, url={Url}.",
            resultado.ObjectKey, resultado.Url);

        return Result<UploadImagemResponse>.Ok(new UploadImagemResponse(resultado.Url));
    }
}
