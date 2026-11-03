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
        => throw NaoConfigurado();

    // Sem storage configurado não há objetos para listar: devolve vazio para a
    // limpeza de órfãs virar um no-op silencioso (não faz sentido falhar um job
    // de manutenção só porque o ambiente não tem storage).
    public Task<IReadOnlyList<StoredObject>> ListObjectsAsync(string prefixo, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<StoredObject>>([]);

    public Task DeleteAsync(string objectKey, CancellationToken ct = default)
        => throw NaoConfigurado();

    private static InvalidOperationException NaoConfigurado() => new(
        "Storage de imagens não configurado: defina a seção 'Storage' " +
        "(ServiceUrl, Bucket, credenciais) no appsettings do ambiente.");
}
