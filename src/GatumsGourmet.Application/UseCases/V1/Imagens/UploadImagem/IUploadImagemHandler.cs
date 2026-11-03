using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Imagens.UploadImagem;

/// <summary>
/// Caso de uso de upload de imagem: valida o arquivo (tamanho + tipo real por
/// magic bytes) e delega a gravação ao storage.
/// </summary>
public interface IUploadImagemHandler
{
    /// <summary>
    /// Valida e grava a imagem. <paramref name="contentType"/> e
    /// <paramref name="nomeArquivo"/> vêm do cliente e NÃO são confiáveis para
    /// decidir o tipo — servem apenas como metadado; o tipo real é inferido dos
    /// primeiros bytes do conteúdo.
    /// </summary>
    Task<Result<UploadImagemResponse>> Handle(
        Stream conteudo,
        string? contentType,
        string? nomeArquivo,
        long tamanho,
        CancellationToken ct = default);
}
