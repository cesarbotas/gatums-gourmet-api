namespace GatumsGourmet.Domain.Services.Interfaces;

/// <summary>
/// Resultado de um upload de imagem para o storage: a URL pública de acesso
/// e a chave do objeto no bucket (usada para remoção/organização).
/// </summary>
public record ImageUploadResult(string Url, string ObjectKey);

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
}
