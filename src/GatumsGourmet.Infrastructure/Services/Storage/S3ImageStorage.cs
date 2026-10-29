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
        };

        // NOTA: NÃO usamos DisablePayloadSigning — na V4 ele exige HTTPS e quebra
        // o MinIO local em http://. A compatibilidade com S3-compatíveis vem do
        // RequestChecksumCalculation = WhenRequired no AmazonS3Config (ver DI),
        // que desliga o checksum CRC + aws-chunked que o MinIO rejeita, mantendo
        // a assinatura SigV4 normal (funciona sobre HTTP).
        await s3.PutObjectAsync(request, ct);

        // Monta a URL pública sem barra dupla entre a base e a object key.
        var url = UrlPublica(objectKey);
        return new ImageUploadResult(url, objectKey);
    }

    public async Task<IReadOnlyList<StoredObject>> ListObjectsAsync(
        string prefixo,
        CancellationToken ct = default)
    {
        var resultado = new List<StoredObject>();
        string? continuationToken = null;

        // ListObjectsV2 retorna no máx. 1000 por página; pagina com o token.
        do
        {
            var resp = await s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _options.Bucket,
                Prefix = $"{prefixo}/",
                ContinuationToken = continuationToken,
            }, ct);

            foreach (var o in resp.S3Objects ?? [])
                resultado.Add(new StoredObject(o.Key, UrlPublica(o.Key), o.LastModified?.ToUniversalTime() ?? DateTime.UtcNow));

            continuationToken = resp.IsTruncated == true ? resp.NextContinuationToken : null;
        }
        while (continuationToken is not null);

        return resultado;
    }

    public Task DeleteAsync(string objectKey, CancellationToken ct = default) =>
        s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
        }, ct);

    private string UrlPublica(string objectKey) =>
        $"{_options.PublicBaseUrl.TrimEnd('/')}/{objectKey}";
}
