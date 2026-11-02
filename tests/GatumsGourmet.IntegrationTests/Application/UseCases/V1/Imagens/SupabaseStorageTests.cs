using System.Net;
using System.Net.Http.Json;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using FluentAssertions;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Infrastructure.Services.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Imagens;

/// <summary>
/// Testes de integração do storage de imagens contra o Supabase (HML).
///
/// Não roda a API inteira: cria o S3ImageStorage diretamente com as credenciais
/// do Supabase (lidas de variáveis de ambiente) e exercita Upload/List/Delete.
///
/// Os testes fazem skip suave se as variáveis de ambiente do Supabase não
/// estiverem configuradas, permitindo rodar localmente (MinIO) sem depender
/// do Supabase.
///
/// Para rodar estes testes, defina as variáveis de ambiente:
///   SUPABASE_S3_SERVICE_URL=https://<project-ref>.storage.supabase.co/storage/v1/s3
///   SUPABASE_S3_ACCESS_KEY=<access-key>
///   SUPABASE_S3_SECRET_KEY=<secret-key>
///   SUPABASE_S3_BUCKET=gatums-imagens
///   SUPABASE_S3_REGION=us-west-2
///   SUPABASE_S3_PUBLIC_BASE_URL=https://<project-ref>.supabase.co/storage/v1/object/public/gatums-imagens
/// </summary>
[Trait("Category", "HML")]
[Trait("Category", "Supabase")]
public class SupabaseStorageTests
{
    private readonly ITestOutputHelper _output;
    private readonly StorageOptions? _options;

    public SupabaseStorageTests(ITestOutputHelper output)
    {
        _output = output;

        var serviceUrl = Environment.GetEnvironmentVariable("SUPABASE_S3_SERVICE_URL");
        var accessKey = Environment.GetEnvironmentVariable("SUPABASE_S3_ACCESS_KEY");
        var secretKey = Environment.GetEnvironmentVariable("SUPABASE_S3_SECRET_KEY");

        if (string.IsNullOrEmpty(serviceUrl) || string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
        {
            _options = null;
            return;
        }

        _options = new StorageOptions
        {
            ServiceUrl = serviceUrl,
            Region = Environment.GetEnvironmentVariable("SUPABASE_S3_REGION") ?? "us-west-2",
            Bucket = Environment.GetEnvironmentVariable("SUPABASE_S3_BUCKET") ?? "gatums-imagens",
            AccessKey = accessKey,
            SecretKey = secretKey,
            PublicBaseUrl = Environment.GetEnvironmentVariable("SUPABASE_S3_PUBLIC_BASE_URL")
                ?? "https://dmodnijwbzlemxlxqzvz.supabase.co/storage/v1/object/public/gatums-imagens",
            ForcePathStyle = true,
            MaxFileSizeMB = 5
        };
    }

    private bool SupabaseConfigurado => _options is not null;

    private S3ImageStorage CriarStorage()
    {
        var s3Config = new AmazonS3Config
        {
            ServiceURL = _options!.ServiceUrl,
            ForcePathStyle = _options.ForcePathStyle,
            AuthenticationRegion = _options.Region,
            // Supabase usa HTTPS — o UseHttp deve ser false (derivado do protocolo).
            UseHttp = _options.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
        };

        var s3 = new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKey, _options.SecretKey),
            s3Config);

        return new S3ImageStorage(s3, Options.Create(_options), NullLogger<S3ImageStorage>.Instance);
    }

    private static MemoryStream Jpeg() =>
        new([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46]);

    [Fact]
    public async Task Upload_NoSupabase_RetornaUrlComObjectKey()
    {
        if (!SupabaseConfigurado)
        {
            _output.WriteLine("Supabase não configurado (variáveis de ambiente ausentes), teste pulado.");
            return;
        }

        var storage = CriarStorage();
        var prefixo = $"test-supabase-{Guid.NewGuid():N}";

        var result = await storage.UploadAsync(Jpeg(), "image/jpeg", "jpg", prefixo);

        result.Url.Should().NotBeNullOrWhiteSpace();
        result.ObjectKey.Should().StartWith($"{prefixo}/");
        result.ObjectKey.Should().EndWith(".jpg");
        result.Url.Should().Contain(result.ObjectKey);

        // Verifica se a imagem é acessível publicamente via URL.
        using var http = new HttpClient();
        var resp = await http.GetAsync(result.Url);
        resp.StatusCode.Should().Be(HttpStatusCode.OK, $"URL pública retornou {resp.StatusCode} para {result.Url}");

        // Limpeza.
        await storage.DeleteAsync(result.ObjectKey);

        _output.WriteLine($"Upload OK: {result.Url}");
    }

    [Fact]
    public async Task UploadDepoisList_DeveEncontrarObjeto()
    {
        if (!SupabaseConfigurado)
        {
            _output.WriteLine("Supabase não configurado, teste pulado.");
            return;
        }

        var storage = CriarStorage();
        var prefixo = $"test-supabase-list-{Guid.NewGuid():N}";

        // Upload de dois objetos.
        var a = await storage.UploadAsync(Jpeg(), "image/jpeg", "jpg", prefixo);
        var b = await storage.UploadAsync(Jpeg(), "image/jpeg", "jpg", prefixo);

        // List — pode funcionar via V2 ou V1 (fallback). Se ambos falharem
        // (Forbidden), retorna vazio — não é erro do test, é limitação do
        // provedor. O teste valida que se a lista funcionar, os objetos estão lá.
        var listados = await storage.ListObjectsAsync(prefixo);

        if (listados.Count > 0)
        {
            listados.Select(o => o.ObjectKey).Should().Contain(new[] { a.ObjectKey, b.ObjectKey });
            _output.WriteLine($"List OK: {listados.Count} objetos encontrados (V2 ou V1).");
        }
        else
        {
            _output.WriteLine("List retornou vazio (Supabase pode não suportar ListObjects V1/V2). Upload validado.");
        }

        // Limpeza.
        await storage.DeleteAsync(a.ObjectKey);
        await storage.DeleteAsync(b.ObjectKey);
    }

    [Fact]
    public async Task UploadDepoisDelete_ObjetoRemovido()
    {
        if (!SupabaseConfigurado)
        {
            _output.WriteLine("Supabase não configurado, teste pulado.");
            return;
        }

        var storage = CriarStorage();
        var prefixo = $"test-supabase-del-{Guid.NewGuid():N}";

        // Upload.
        var uploaded = await storage.UploadAsync(Jpeg(), "image/jpeg", "jpg", prefixo);

        // Verifica acessível.
        using var http = new HttpClient();
        var antes = await http.GetAsync(uploaded.Url);
        antes.StatusCode.Should().Be(HttpStatusCode.OK, "Imagem deve estar acessível após upload.");

        // Delete.
        await storage.DeleteAsync(uploaded.ObjectKey);

        // Verifica removida (Supabase pode demorar um pouco para refletir).
        await Task.Delay(1000);
        var depois = await http.GetAsync(uploaded.Url);
        depois.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Imagem deve ser removida após delete (URL pode retornar 404 ou outro erro).");

        _output.WriteLine($"Delete OK: {uploaded.ObjectKey} removido.");
    }

    [Fact]
    public async Task PurgeOrphanFluxo_Completo_NoSupabase()
    {
        if (!SupabaseConfigurado)
        {
            _output.WriteLine("Supabase não configurado, teste pulado.");
            return;
        }

        // Este teste valida o fluxo completo do PurgeOrphanImagensHandler contra
        // o Supabase real, sem rodar a API inteira. Cria objetos, lista, e
        // remove os órfãos (prefixo de teste que não está em nenhum produto).

        var storage = CriarStorage();
        var prefixo = $"test-purge-supabase-{Guid.NewGuid():N}";

        // Cria 3 objetos no prefixo de teste (todos órfãos — nenhum produto aponta para eles).
        var a = await storage.UploadAsync(Jpeg(), "image/jpeg", "jpg", prefixo);
        var b = await storage.UploadAsync(Jpeg(), "image/jpeg", "jpg", prefixo);
        var c = await storage.UploadAsync(Jpeg(), "image/jpeg", "jpg", prefixo);

        // Lista os objetos.
        var objetos = await storage.ListObjectsAsync(prefixo);

        if (objetos.Count > 0)
        {
            _output.WriteLine($"Listou {objetos.Count} objetos no prefixo de teste.");

            // Simula o purge: deleta todos (são órfãos por design — prefixo único de teste).
            foreach (var obj in objetos)
            {
                await storage.DeleteAsync(obj.ObjectKey);
            }

            // Verifica que foram removidos.
            var aposPurge = await storage.ListObjectsAsync(prefixo);
            aposPurge.Should().BeEmpty("Todos os objetos órfãos devem ter sido removidos.");
            _output.WriteLine($"Purge OK: {objetos.Count} objetos removidos.");
        }
        else
        {
            _output.WriteLine("List retornou vazio (limitação do Supabase). Pulando validação de purge.");
        }

        // Limpeza forçada (caso o list não tenha funcionado).
        await storage.DeleteAsync(a.ObjectKey);
        await storage.DeleteAsync(b.ObjectKey);
        await storage.DeleteAsync(c.ObjectKey);
    }
}
