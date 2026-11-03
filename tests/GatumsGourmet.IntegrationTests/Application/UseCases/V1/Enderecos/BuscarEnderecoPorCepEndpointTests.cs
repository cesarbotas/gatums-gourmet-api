using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.IntegrationTests.Factory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Enderecos;

[Collection("Integration")]
public class BuscarEnderecoPorCepEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public BuscarEnderecoPorCepEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    // Substitui o ICepProvider real (ViaCEP) por um fake, para o teste ser
    // determinístico e offline — não bate na API pública.
    private HttpClient ClientComCepProvider(ICepProvider fake) =>
        _factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s =>
            {
                s.RemoveAll<ICepProvider>();
                s.AddScoped(_ => fake);
            })).CreateClient();

    private sealed class FakeCepProvider(EnderecoCep? resultado) : ICepProvider
    {
        public Task<EnderecoCep?> BuscarAsync(string cep, CancellationToken ct = default) =>
            Task.FromResult(resultado);
    }

    [Fact]
    public async Task Cep_Valido_RetornaEndereco()
    {
        var fake = new FakeCepProvider(
            new EnderecoCep("01001000", "Praça da Sé", "lado ímpar", "Sé", "São Paulo", "SP"));
        var client = ClientComCepProvider(fake);

        var resp = await client.GetAsync("/api/v1/enderecos/cep/01001000");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<EnderecoCepDto>();
        body!.Cep.Should().Be("01001000");
        body.Logradouro.Should().Be("Praça da Sé");
        body.Cidade.Should().Be("São Paulo");
        body.Estado.Should().Be("SP");
    }

    [Fact]
    public async Task Cep_Publico_NaoExigeAutenticacao()
    {
        var fake = new FakeCepProvider(
            new EnderecoCep("01001000", "Praça da Sé", null, "Sé", "São Paulo", "SP"));
        var client = ClientComCepProvider(fake); // sem Bearer

        var resp = await client.GetAsync("/api/v1/enderecos/cep/01001000");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cep_NaoEncontrado_RetornaBadRequest()
    {
        var client = ClientComCepProvider(new FakeCepProvider(null));

        var resp = await client.GetAsync("/api/v1/enderecos/cep/99999999");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cep_FormatoInvalido_RetornaBadRequest()
    {
        // Não precisa chegar ao provider — o handler valida o formato antes.
        var client = ClientComCepProvider(new FakeCepProvider(null));

        var resp = await client.GetAsync("/api/v1/enderecos/cep/123");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record EnderecoCepDto(
        string Cep, string Logradouro, string? Complemento, string Bairro, string Cidade, string Estado);
}
