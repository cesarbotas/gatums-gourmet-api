using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Infrastructure.Services.Storage;

/// <summary>
/// Implementação de fallback de <see cref="IImageStorage"/> registrada quando a
/// seção <c>Storage</c> não está configurada. Em vez de deixar o endpoint de
/// upload estourar um <see cref="NullReferenceException"/> ou um 500 opaco, ela
/// falha de forma explícita e diagnosticável ao ser efetivamente usada — e o
/// host sobe normalmente mesmo sem storage configurado.
/// </summary>
public sealed class UnconfiguredImageStorage : IImageStorage
{
    public Task<ImageUploadResult> UploadAsync(
        Stream conteudo,
        string contentType,
        string extensao,
        string prefixo,
        CancellationToken ct = default)
        => throw new InvalidOperationException(
            "Storage de imagens não configurado: defina a seção 'Storage' " +
            "(ServiceUrl, Bucket, credenciais) no appsettings do ambiente.");
}
