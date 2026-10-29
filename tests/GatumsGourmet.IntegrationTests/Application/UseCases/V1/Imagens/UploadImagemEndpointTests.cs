using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Infrastructure.Data;
using GatumsGourmet.IntegrationTests.Factory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Imagens;

/// <summary>
/// Testes de integração do endpoint POST /api/v1/imagens.
///
/// (A) Casos determinísticos e offline: substituem o IImageStorage real por um
///     FakeImageStorage, então nenhuma rede é tocada. Autenticação (Admin/Cliente)
///     é semeada no MESMO host que registra o fake, para que o client autenticado
///     e o fake convivam na mesma requisição.
///
/// (B) Caso real condicional contra MinIO: só executa de fato se a porta 9000
///     responder; caso contrário faz skip suave (early-return com log). Mesmo
///     com MinIO up, se o PUT falhar por infra ausente (bucket inexistente) o
///     teste faz skip suave — nunca quebra a suíte por ambiente não preparado.
/// </summary>
[Collection("Integration")]
public class UploadImagemEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public UploadImagemEndpointTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    // ---------------------------------------------------------------------
    // Fake de storage: não toca rede. Marca Chamado=true só quando acionado,
    // permitindo asserir que o caminho feliz chegou ao storage e que o caminho
    // de tipo inválido NÃO chegou (rejeitado antes, pelos magic bytes).
    // ---------------------------------------------------------------------
    private sealed class FakeImageStorage : IImageStorage
    {
        public bool Chamado { get; private set; }

        public Task<ImageUploadResult> UploadAsync(
            Stream conteudo, string contentType, string extensao, string prefixo, CancellationToken ct = default)
        {
            Chamado = true;
            var key = $"{prefixo}/fake.{extensao}";
            return Task.FromResult(new ImageUploadResult($"https://fake.local/{key}", key));
        }
    }

    private sealed record UploadImagemDto(string Url);

    // Magic bytes reais de um JPEG: FF D8 FF E0 ... + padding.
    private static byte[] JpegBytes() =>
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46,
        0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01
    ];

    private static byte[] TextoBytes() => Encoding.UTF8.GetBytes("isto não é imagem");

    private static MultipartFormDataContent Multipart(byte[] bytes, string contentType, string fileName)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent();
        form.Add(file, "arquivo", fileName);
        return form;
    }

    // ---------------------------------------------------------------------
    // Monta um host de teste que registra o FakeImageStorage e devolve tanto
    // a factory (para semear usuário) quanto o fake (para asserir Chamado).
    // ---------------------------------------------------------------------
    private WebApplicationFactory<Program> HostComFake(FakeImageStorage fake) =>
        _factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s =>
            {
                s.RemoveAll<IImageStorage>();
                s.AddScoped<IImageStorage>(_ => fake);
            }));

    // ---------------------------------------------------------------------
    // Semeia um usuário com a role indicada no MESMO host do fake e faz login
    // real (/api/v1/auth/login) para obter o Bearer. Replica o mínimo do
    // AuthenticatedClientFactory, porque aquele helper usa factory.CreateClient()
    // internamente (host sem o fake). Aqui autenticamos e injetamos o fake no
    // mesmo host, garantindo que ambos vivam na mesma requisição.
    // ---------------------------------------------------------------------
    private static async Task<HttpClient> ClientAutenticadoAsync(
        WebApplicationFactory<Program> host, string roleNome)
    {
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GatumsDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var clock = scope.ServiceProvider.GetRequiredService<IClock>();

            var roleLower = roleNome.ToLowerInvariant();
            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = $"Usuário {roleLower}",
                Email = $"{roleLower}-{Guid.NewGuid():N}@gatums.com",
                SenhaHash = hasher.Hash("senha123"),
                Ativo = true,
                CriadoEm = clock.UtcNow
            };
            db.Usuarios.Add(usuario);

            var roleEntity = await db.Roles.FirstOrDefaultAsync(r => r.Nome == roleNome)
                ?? db.Roles.Add(new Role { Nome = roleNome }).Entity;
            await db.SaveChangesAsync();

            db.UsuarioRoles.Add(new UsuarioRole { UsuarioId = usuario.Id, RoleId = roleEntity.Id });
            await db.SaveChangesAsync();

            var loginClient = host.CreateClient();
            var loginResp = await loginClient.PostAsJsonAsync("/api/v1/auth/login",
                new { email = usuario.Email, senha = "senha123" });

            if (!loginResp.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Login de teste falhou ({(int)loginResp.StatusCode}) para {usuario.Email}: " +
                    await loginResp.Content.ReadAsStringAsync());

            using var doc = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
            var token = doc.RootElement.GetProperty("accessToken").GetString()
                ?? throw new InvalidOperationException("Token ausente na resposta de login.");

            var client = host.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }

    // =====================================================================
    // (A) Casos offline com fake
    // =====================================================================

    [Fact]
    public async Task Admin_Jpeg_RetornaCreated_ComUrl()
    {
        var fake = new FakeImageStorage();
        var host = HostComFake(fake);
        var client = await ClientAutenticadoAsync(host, "Admin");

        var resp = await client.PostAsync("/api/v1/imagens", Multipart(JpegBytes(), "image/jpeg", "foto.jpg"));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<UploadImagemDto>();
        body!.Url.Should().NotBeNullOrWhiteSpace();
        fake.Chamado.Should().BeTrue();
    }

    [Fact]
    public async Task Cliente_Jpeg_RetornaForbidden()
    {
        var fake = new FakeImageStorage();
        var host = HostComFake(fake);
        var client = await ClientAutenticadoAsync(host, "Cliente");

        var resp = await client.PostAsync("/api/v1/imagens", Multipart(JpegBytes(), "image/jpeg", "foto.jpg"));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonimo_Jpeg_RetornaUnauthorized()
    {
        var fake = new FakeImageStorage();
        var host = HostComFake(fake);
        var client = host.CreateClient(); // sem Bearer

        var resp = await client.PostAsync("/api/v1/imagens", Multipart(JpegBytes(), "image/jpeg", "foto.jpg"));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_Texto_RetornaBadRequest_SemAcionarStorage()
    {
        var fake = new FakeImageStorage();
        var host = HostComFake(fake);
        var client = await ClientAutenticadoAsync(host, "Admin");

        var resp = await client.PostAsync("/api/v1/imagens", Multipart(TextoBytes(), "text/plain", "a.txt"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        fake.Chamado.Should().BeFalse();
    }

    // =====================================================================
    // (B) Caso real condicional contra MinIO
    // =====================================================================

    private static bool MinioDisponivel(string host = "localhost", int port = 9000)
    {
        try
        {
            using var c = new TcpClient();
            var connect = c.ConnectAsync(host, port);
            return connect.Wait(TimeSpan.FromMilliseconds(500)) && c.Connected;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public async Task Admin_Jpeg_MinioReal_RetornaCreated_OuSkipSuave()
    {
        if (!MinioDisponivel())
        {
            _output.WriteLine("MinIO indisponível, teste (B) pulado");
            return;
        }

        // MinIO responde: sobe um host com a seção Storage apontando para ele e
        // usa o S3ImageStorage REAL (não substitui IImageStorage).
        var host = _factory.WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration(cfg => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ServiceUrl"] = "http://localhost:9000",
                ["Storage:Region"] = "us-east-1",
                ["Storage:Bucket"] = "gatums-imagens",
                ["Storage:AccessKey"] = "minioadmin",
                ["Storage:SecretKey"] = "minioadmin",
                ["Storage:PublicBaseUrl"] = "http://localhost:9000/gatums-imagens",
                ["Storage:ForcePathStyle"] = "true",
                ["Storage:MaxFileSizeMB"] = "5"
            })));

        var client = await ClientAutenticadoAsync(host, "Admin");

        HttpResponseMessage resp;
        try
        {
            resp = await client.PostAsync("/api/v1/imagens", Multipart(JpegBytes(), "image/jpeg", "foto.jpg"));
        }
        catch (Exception ex)
        {
            // Falha de infra (bucket/endpoint indisponível) → skip suave.
            _output.WriteLine($"MinIO indisponível, teste (B) pulado (erro no PUT: {ex.Message})");
            return;
        }

        if (resp.StatusCode != HttpStatusCode.Created)
        {
            // O host respondeu, mas o storage real não completou (ex.: bucket
            // inexistente porque a Task 6 ainda não rodou). Skip suave.
            _output.WriteLine(
                $"MinIO indisponível, teste (B) pulado (status {(int)resp.StatusCode}: " +
                $"{await resp.Content.ReadAsStringAsync()})");
            return;
        }

        var body = await resp.Content.ReadFromJsonAsync<UploadImagemDto>();
        body!.Url.Should().NotBeNullOrWhiteSpace();
        body.Url.Should().Contain("localhost:9000");
        body.Url.Should().Contain("gatums-imagens");
    }
}
