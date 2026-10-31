namespace GatumsGourmet.Domain.Services.Interfaces;

/// <summary>
/// Resultado de um upload de imagem para o storage: a URL pública de acesso
/// e a chave do objeto no bucket (usada para remoção/organização).
/// </summary>
public record ImageUploadResult(string Url, string ObjectKey);

/// <summary>
/// Um objeto existente no storage: a chave, a URL pública e a data da última
/// modificação (usada pela limpeza de órfãs para aplicar o período de carência).
/// </summary>
public record StoredObject(string ObjectKey, string Url, DateTime LastModifiedUtc);

/// <summary>
/// Abstração de um storage de imagens (S3-compatível). A implementação vive
/// na Infrastructure e é configurada por ambiente.
/// </summary>
public interface IImageStorage
{
    /// <summary>
    /// Envia o conteúdo da imagem para o storage sob o <paramref name="prefixo"/>
    /// informado e retorna a URL pública e a chave do objeto.
    /// </summary>
    Task<ImageUploadResult> UploadAsync(
        Stream conteudo,
        string contentType,
        string extensao,
        string prefixo,
        CancellationToken ct = default);

    /// <summary>Lista os objetos existentes sob o <paramref name="prefixo"/>.</summary>
    Task<IReadOnlyList<StoredObject>> ListObjectsAsync(
        string prefixo,
        CancellationToken ct = default);

    /// <summary>Remove o objeto de chave <paramref name="objectKey"/> do storage.</summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);
}
