using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GatumsGourmet.Application.UseCases.V1.Imagens.UploadImagem;

/// <summary>
/// Valida o arquivo recebido (tamanho máximo configurável + tipo real por
/// magic bytes) e, sendo válido, delega a gravação ao <see cref="IImageStorage"/>.
/// </summary>
public sealed class UploadImagemHandler(IImageStorage storage, IConfiguration config)
    : IUploadImagemHandler
{
    public async Task<Result<UploadImagemResponse>> Handle(
        Stream conteudo,
        string? contentType,
        string? nomeArquivo,
        long tamanho,
        CancellationToken ct = default)
    {
        var maxMb = config.GetValue<int?>("Storage:MaxFileSizeMB") ?? 5;

        if (tamanho <= 0)
            return Result<UploadImagemResponse>.Fail("Arquivo vazio.");

        if (tamanho > maxMb * 1024L * 1024L)
            return Result<UploadImagemResponse>.Fail(
                $"Arquivo excede o tamanho máximo de {maxMb} MB.");

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
            return Result<UploadImagemResponse>.Fail(
                "Tipo de imagem não suportado. Use JPEG, PNG ou WebP.");

        ms.Position = 0;
        var resultado = await storage.UploadAsync(ms, ctype, extensao, "produtos", ct);

        return Result<UploadImagemResponse>.Ok(new UploadImagemResponse(resultado.Url));
    }
}
