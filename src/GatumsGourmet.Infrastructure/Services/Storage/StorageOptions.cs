namespace GatumsGourmet.Infrastructure.Services.Storage;

/// <summary>
/// Opções de configuração do storage de imagens (S3-compatível). Mapeiam a
/// seção <c>Storage</c> do appsettings; os valores mudam por ambiente
/// (MinIO local / Supabase homologação / Cloudflare R2 produção).
/// </summary>
public sealed class StorageOptions
{
    /// <summary>Endpoint S3 do provedor (ex.: <c>http://localhost:9000</c> no MinIO).</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>Região informada ao cliente S3 (ex.: <c>us-east-1</c>).</summary>
    public string Region { get; set; } = "us-west-2";

    /// <summary>Nome do bucket onde as imagens são gravadas.</summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>Chave de acesso (injetada por variável de ambiente fora do local).</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>Chave secreta (injetada por variável de ambiente fora do local).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Base usada para montar a URL pública retornada ao cliente
    /// (ex.: <c>http://localhost:9000/gatums-imagens</c>).
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Usa path-style nas requisições (<c>true</c> para MinIO e R2). Necessário
    /// quando o provedor não suporta buckets como subdomínio.
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>Tamanho máximo do arquivo em MB (o use case também lê este valor).</summary>
    public int MaxFileSizeMB { get; set; } = 5;

    /// <summary>
    /// Habilita/desabilita a limpeza periódica de imagens órfãs. Desabilitar em
    /// provedores que não suportam ListObjectsV2 (ex.: Supabase S3 API).
    /// Default: true (compatível com MinIO e R2 que suportam listagem).
    /// </summary>
    public bool OrphanCleanupEnabled { get; set; } = true;
}
