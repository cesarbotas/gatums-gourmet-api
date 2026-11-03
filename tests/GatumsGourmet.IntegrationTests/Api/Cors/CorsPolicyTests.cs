using System.Net.Http.Headers;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Api.Cors;

/// <summary>
/// Comportamento do CORS fora de Development: a policy só devolve o header
/// Access-Control-Allow-Origin para origens que casam com as origens exatas
/// (<c>Cors:AllowedOrigins</c>) ou com os padrões curinga
/// (<c>Cors:AllowedOriginPatterns</c>, ex.: <c>https://gatums-gourmet*.vercel.app</c>).
/// O ambiente de teste é "Test" (não-Development), então a policy restritiva vale.
/// </summary>
[Collection("Integration")]
public class CorsPolicyTests
{
    private readonly CustomWebApplicationFactory _factory;

    public CorsPolicyTests(CustomWebApplicationFactory factory) => _factory = factory;

    // Sobe um host com origens exatas (PRD) + padrão curinga (HML Vercel).
    private HttpClient ClientComCors() =>
        _factory.WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration(cfg => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://app.gatumsgourmet.com.br",
                ["Cors:AllowedOriginPatterns:0"] = "https://gatums-gourmet*.vercel.app"
            }))).CreateClient();

    private async Task<HttpResponseMessage> GetComOrigin(HttpClient client, string origin)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/produtos");
        req.Headers.Add("Origin", origin);
        return await client.SendAsync(req);
    }

    [Fact]
    public async Task OrigemExata_Prd_RecebeAllowOrigin()
    {
        var resp = await GetComOrigin(ClientComCors(), "https://app.gatumsgourmet.com.br");

        resp.Headers.TryGetValues("Access-Control-Allow-Origin", out var values).Should().BeTrue();
        values!.Should().Contain("https://app.gatumsgourmet.com.br");
    }

    [Fact]
    public async Task OrigemCuringa_HmlVercelFixo_RecebeAllowOrigin()
    {
        var resp = await GetComOrigin(ClientComCors(), "https://gatums-gourmet-hub.vercel.app");

        resp.Headers.TryGetValues("Access-Control-Allow-Origin", out var values).Should().BeTrue();
        values!.Should().Contain("https://gatums-gourmet-hub.vercel.app");
    }

    [Fact]
    public async Task OrigemCuringa_HmlVercelPreview_RecebeAllowOrigin()
    {
        var resp = await GetComOrigin(ClientComCors(),
            "https://gatums-gourmet-36p86p4pl-cesar-botas-projects.vercel.app");

        resp.Headers.TryGetValues("Access-Control-Allow-Origin", out var values).Should().BeTrue();
    }

    [Fact]
    public async Task OrigemNaoListada_NaoRecebeAllowOrigin()
    {
        var resp = await GetComOrigin(ClientComCors(), "https://malicioso.com");

        resp.Headers.TryGetValues("Access-Control-Allow-Origin", out _).Should().BeFalse();
    }

    [Fact]
    public async Task OrigemVercelDeOutroProjeto_NaoRecebeAllowOrigin()
    {
        // O padrão é gatums-gourmet*.vercel.app — não pode virar "qualquer vercel.app".
        var resp = await GetComOrigin(ClientComCors(), "https://outro-projeto.vercel.app");

        resp.Headers.TryGetValues("Access-Control-Allow-Origin", out _).Should().BeFalse();
    }

    [Fact]
    public void Release_SemOrigensConfiguradas_FalhaNoStartup()
    {
        // Fail-fast: subir em Release/PRD sem nenhuma origem/padrão deve estourar
        // no startup, não passar silenciosamente com CORS negando tudo.
        var factory = _factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Release");
            b.ConfigureAppConfiguration(cfg => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "",
                ["Cors:AllowedOriginPatterns:0"] = ""
            }));
        });

        // CreateClient dispara o build do host + pipeline (UseGatumsCors força a
        // resolução das opções) → deve lançar.
        var act = () => factory.CreateClient();

        act.Should().Throw<Exception>()
            .Where(e => e.Message.Contains("CORS") || (e.InnerException != null && e.InnerException.Message.Contains("CORS")));
    }

    [Fact]
    public void Release_OrigemComPath_FalhaNoStartup()
    {
        // Validação de formato: origem com PATH é inválida — o header Origin do
        // browser é só esquema+host+porta. (Barra final sozinha é tolerada: a
        // extension normaliza removendo-a.)
        var factory = _factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Release");
            b.ConfigureAppConfiguration(cfg => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://app.gatumsgourmet.com.br/app"
            }));
        });

        var act = () => factory.CreateClient();

        act.Should().Throw<Exception>();
    }

    [Fact]
    public async Task Release_OrigemComBarraFinal_ToleradaENormalizada()
    {
        // Barra final é tolerada (normalizada). A origem configurada com "/" no fim
        // ainda casa a requisição (cujo header Origin vem sem a barra).
        var client = _factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Release");
            b.ConfigureAppConfiguration(cfg => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://app.gatumsgourmet.com.br/"
            }));
        }).CreateClient();

        var resp = await GetComOrigin(client, "https://app.gatumsgourmet.com.br");

        resp.Headers.TryGetValues("Access-Control-Allow-Origin", out var values).Should().BeTrue();
        values!.Should().Contain("https://app.gatumsgourmet.com.br");
    }
}
