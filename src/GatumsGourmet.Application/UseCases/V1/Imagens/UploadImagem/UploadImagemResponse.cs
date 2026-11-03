namespace GatumsGourmet.Application.UseCases.V1.Imagens.UploadImagem;

/// <summary>
/// Resultado do upload de uma imagem: a URL pública de acesso ao arquivo.
/// </summary>
public record UploadImagemResponse(string Url);
