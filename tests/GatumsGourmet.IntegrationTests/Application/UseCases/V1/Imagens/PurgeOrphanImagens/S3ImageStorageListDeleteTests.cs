using System.Net.Sockets;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using FluentAssertions;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Infrastructure.Services.Storage;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Imagens.PurgeOrphanImagens;

/// <summary>
/// Exercita o List/Delete reais do <see cref="S3ImageStorage"/> contra o MinIO
/// (como o teste real de upload). Pulado com skip suave se a porta 9000 não
/// responder. Prova que a limpeza de órfãs consegue enumerar e remover objetos.
/// </summary>
public class S3ImageStorageListDeleteTests
{
    private readonly ITestOutputHelper _output;

    public S3ImageStorageListDeleteTests(ITestOutputHelper output) => _output = output;

    private static bool MinioDisponivel(string host = "localhost", int port = 9000)
    {
        try
        {
            using var c = new TcpClient();
            return c.ConnectAsync(host, port).Wait(TimeSpan.FromMilliseconds(500)) && c.Connected;
        }
        catch { return false; }
    }

    private static S3ImageStorage CriarStorage()
    {
        var options = new StorageOptions
        {
            ServiceUrl = "http://localhost:9000",
            Region = "us-east-1",
            Bucket = "gatums-imagens",
            AccessKey = "minioadmin",
            SecretKey = "minioadmin",
            PublicBaseUrl = "http://localhost:9000/gatums-imagens",
            ForcePathStyle = true,
            MaxFileSizeMB = 5
        };
        var s3 = new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKey, options.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                ForcePathStyle = options.ForcePathStyle,
                AuthenticationRegion = options.Region,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED
            });
        return new S3ImageStorage(s3, Options.Create(options));
    }

    private static MemoryStream Jpeg() =>
        new([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46]);

    [Fact]
    public async Task UploadListDelete_ContraMinIO_RemoveObjeto()
    {
        if (!MinioDisponivel())
        {
            _output.WriteLine("MinIO indisponível (porta 9000 fechada), teste pulado");
            return;
        }

        var storage = CriarStorage();

        // Usa um prefixo único por execução para o teste não enxergar lixo de
        // outras rodadas nem interferir no bucket compartilhado.
        var prefixo = $"test-purge-{Guid.NewGuid():N}";

        var a = await storage.UploadAsync(Jpeg(), "image/jpeg", "jpg", prefixo);
        var b = await storage.UploadAsync(Jpeg(), "image/jpeg", "jpg", prefixo);

        var listados = await storage.ListObjectsAsync(prefixo);
        listados.Select(o => o.ObjectKey).Should().Contain(new[] { a.ObjectKey, b.ObjectKey });
        listados.Should().OnlyContain(o => o.LastModifiedUtc != default);

        await storage.DeleteAsync(a.ObjectKey);

        var aposDelete = await storage.ListObjectsAsync(prefixo);
        aposDelete.Select(o => o.ObjectKey).Should().NotContain(a.ObjectKey);
        aposDelete.Select(o => o.ObjectKey).Should().Contain(b.ObjectKey);

        // Limpeza: remove o objeto restante do bucket compartilhado.
        await storage.DeleteAsync(b.ObjectKey);
    }
}
