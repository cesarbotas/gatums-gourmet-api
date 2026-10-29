using Amazon.S3;
using Amazon.S3.Model;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace GatumsGourmet.Infrastructure.Services.Storage;

/// <summary>
/// Implementação de <see cref="IImageStorage"/> sobre qualquer backend
/// S3-compatível (MinIO, Supabase Storage, Cloudflare R2) via AWSSDK.S3. O
/// <see cref="IAmazonS3"/> é injetado (ver registro em AddInfrastructure) para
/// testabilidade; esta classe só monta a object key, envia o conteúdo e devolve
/// a URL pública.
/// </summary>
public sealed class S3ImageStorage(IAmazonS3 s3, IOptions<StorageOptions> options) : IImageStorage
{
    private readonly StorageOptions _options = options.Value;

    public async Task<ImageUploadResult> UploadAsync(
        Stream conteudo,
        string contentType,
        string extensao,
        string prefixo,
        CancellationToken ct = default)
    {
        // ImageTypeDetector retorna a extensão SEM ponto ("jpg"/"png"/"webp"):
        // o ponto é adicionado aqui ao montar a chave do objeto.
        var objectKey = $"{prefixo}/{Guid.NewGuid():N}.{extensao}";

        var request = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            InputStream = conteudo,
            ContentType = contentType,
            AutoCloseStream = false,
            // Path-style (MinIO/R2) não lida bem com a assinatura de payload
            // chunked da V4; desabilitá-la mantém o PUT compatível.
            DisablePayloadSigning = _options.ForcePathStyle,
        };

        await s3.PutObjectAsync(request, ct);

        // Monta a URL pública sem barra dupla entre a base e a object key.
        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/{objectKey}";
        return new ImageUploadResult(url, objectKey);
    }
}
