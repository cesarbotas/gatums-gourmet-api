using System.Net;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Relatorios.GetVendasResumo;

[Collection("Integration")]
public class GetVendasResumoEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public GetVendasResumoEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task VendasResumo_ComoAdmin_RetornaEstrutura()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var resp = await admin.GetAsync("/api/v1/relatorios/vendas/resumo");

        resp.IsSuccessStatusCode.Should().BeTrue();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.TryGetProperty("faturamento", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("topProdutos", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("serieDiaria", out _).Should().BeTrue();
    }

    [Fact]
    public async Task VendasResumo_ComoCliente_RetornaForbidden()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.GetAsync("/api/v1/relatorios/vendas/resumo");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VendasResumo_SemAutenticacao_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/relatorios/vendas/resumo");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
