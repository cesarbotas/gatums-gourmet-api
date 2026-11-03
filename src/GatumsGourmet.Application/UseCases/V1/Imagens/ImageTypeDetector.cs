namespace GatumsGourmet.Application.UseCases.V1.Imagens;

/// <summary>
/// Detecta o tipo de uma imagem pela assinatura (magic bytes) do início do
/// arquivo. Reconhece JPEG, PNG e WebP; qualquer outra coisa (ou buffer curto
/// demais) retorna <c>Ok=false</c>.
/// </summary>
public static class ImageTypeDetector
{
    /// <summary>
    /// Inspeciona os primeiros bytes de um arquivo e retorna a extensão e o
    /// content type correspondentes.
    /// </summary>
    /// <param name="prefixo">Início do conteúdo do arquivo.</param>
    /// <returns>
    /// <c>Ok=true</c> com a extensão ("jpg"/"png"/"webp") e o content type
    /// quando a assinatura é reconhecida; caso contrário <c>(false, "", "")</c>.
    /// </returns>
    public static (bool Ok, string Extensao, string ContentType) Detectar(ReadOnlySpan<byte> prefixo)
    {
        // JPEG: FF D8 FF
        if (prefixo.Length >= 3 &&
            prefixo[0] == 0xFF && prefixo[1] == 0xD8 && prefixo[2] == 0xFF)
        {
            return (true, "jpg", "image/jpeg");
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (prefixo.Length >= 8 &&
            prefixo[0] == 0x89 && prefixo[1] == 0x50 && prefixo[2] == 0x4E && prefixo[3] == 0x47 &&
            prefixo[4] == 0x0D && prefixo[5] == 0x0A && prefixo[6] == 0x1A && prefixo[7] == 0x0A)
        {
            return (true, "png", "image/png");
        }

        // WebP: "RIFF" (0-3) .... "WEBP" (8-11)
        if (prefixo.Length >= 12 &&
            prefixo[0] == 0x52 && prefixo[1] == 0x49 && prefixo[2] == 0x46 && prefixo[3] == 0x46 &&
            prefixo[8] == 0x57 && prefixo[9] == 0x45 && prefixo[10] == 0x42 && prefixo[11] == 0x50)
        {
            return (true, "webp", "image/webp");
        }

        return (false, string.Empty, string.Empty);
    }
}
