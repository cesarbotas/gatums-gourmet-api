using Amazon.S3;
using Amazon.S3.Model;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GatumsGourmet.Infrastructure.Services.Storage;

/// <summary>
/// Implementação de <see cref="IImageStorage"/> sobre qualquer backend
/// S3-compatível (MinIO, Supabase Storage, Cloudflare R2) via AWSSDK.S3. O
/// <see cref="IAmazonS3"/> é injetado (ver registro em AddInfrastructure) para
/// testabilidade; esta classe só monta a object key, envia o conteúdo e devolve
/// a URL pública.
/// </summary>
public sealed class S3ImageStorage(
    IAmazonS3 s3,
    IOptions<StorageOptions> options,
    ILogger<S3ImageStorage> logger) : IImageStorage
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

        // ── 1ª tentativa: ListObjectsV2 ──────────────────────
        // V2 é o padrão moderno da AWS, mas alguns provedores S3-compatíveis
        // (ex.: Supabase) podem não suportá-lo. Se falhar com Forbidden, cai
        // no fallback para ListObjects v1, que é mais amplamente suportado.
        try
        {
            string? continuationToken = null;
            do
            {
                var resp = await s3.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = _options.Bucket,
                    Prefix = prefixo,
                    ContinuationToken = continuationToken,
                }, ct);

                foreach (var o in resp.S3Objects ?? [])
                    resultado.Add(new StoredObject(o.Key, UrlPublica(o.Key), o.LastModified.ToUniversalTime()));

                continuationToken = resp.IsTruncated == true ? resp.NextContinuationToken : null;
            }
            while (continuationToken is not null);

            logger.LogInformation("ListObjectsV2: {Count} objetos no prefixo '{Prefixo}' (bucket {Bucket}).",
                resultado.Count, prefixo, _options.Bucket);
            return resultado;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            logger.LogError(ex, "ListObjectsV2 falhou (Forbidden) no bucket {Bucket}. " +
                "ServiceUrl={ServiceUrl}, Region={Region}, ErrorCode={ErrorCode}. " +
                "Tentando ListObjects v1 como fallback...",
                _options.Bucket, _options.ServiceUrl, _options.Region, ex.ErrorCode);
        }

        // ── 2ª tentativa: ListObjects v1 (fallback) ─────────
        // V1 usa Marker/NextMarker em vez de ContinuationToken. É o método
        // mais compatível com provedores S3 que não implementaram V2.
        try
        {
            string? marker = null;
            do
            {
                var resp = await s3.ListObjectsAsync(new ListObjectsRequest
                {
                    BucketName = _options.Bucket,
                    Prefix = prefixo,
                    Marker = marker,
                }, ct);

                foreach (var o in resp.S3Objects ?? [])
                    resultado.Add(new StoredObject(o.Key, UrlPublica(o.Key), o.LastModified.ToUniversalTime()));

                marker = resp.IsTruncated == true ? resp.NextMarker : null;
            }
            while (marker is not null);

            logger.LogInformation("ListObjects v1 (fallback): {Count} objetos no prefixo '{Prefixo}' (bucket {Bucket}).",
                resultado.Count, prefixo, _options.Bucket);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "ListObjects v1 também falhou no bucket {Bucket}. " +
                "ErrorCode={ErrorCode}, StatusCode={StatusCode}, Message={Message}. " +
                "A limpeza de imagens órfãs não terá efeito neste ambiente.",
                _options.Bucket, ex.ErrorCode, ex.StatusCode, ex.Message);
        }

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
