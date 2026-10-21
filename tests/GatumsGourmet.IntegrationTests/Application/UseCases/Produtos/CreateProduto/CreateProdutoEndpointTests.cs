using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Produtos.CreateProduto;

[Collection("Integration")]
public class CreateProdutoEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public CreateProdutoEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Criar_SemAutenticacao_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/produtos", CreateProdutoRequestFixture.Simples());

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Criar_ComoCliente_RetornaForbidden()
    {
        var (client, _) = await _factory.CreateClienteClientAsync();

        var resp = await client.PostAsJsonAsync("/api/v1/produtos", CreateProdutoRequestFixture.Simples());

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Criar_ComoAdmin_RetornaCreatedComId()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var resp = await admin.PostAsJsonAsync("/api/v1/produtos", CreateProdutoRequestFixture.Simples());

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }
}
