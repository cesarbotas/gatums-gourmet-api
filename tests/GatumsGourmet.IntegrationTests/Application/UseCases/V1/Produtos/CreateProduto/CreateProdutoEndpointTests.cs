using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Produtos.CreateProduto;

[Collection("Integration")]
public class CreateProdutoEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public CreateProdutoEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    // Cria uma categoria via POST (admin) e devolve o Id, para referenciar no produto.
    private async Task<long> CriarCategoriaAsync()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var catResp = await admin.PostAsJsonAsync("/api/v1/categorias",
            new { nome = $"Cat {Guid.NewGuid():N}", descricao = "d", ativo = true });
        using var catDoc = JsonDocument.Parse(await catResp.Content.ReadAsStringAsync());
        return catDoc.RootElement.GetProperty("id").GetInt64();
    }

    [Fact]
    public async Task Criar_SemAutenticacao_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/produtos", CreateProdutoRequestFixture.Simples(0));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Criar_ComoCliente_RetornaForbidden()
    {
        var (client, _) = await _factory.CreateClienteClientAsync();

        var resp = await client.PostAsJsonAsync("/api/v1/produtos", CreateProdutoRequestFixture.Simples(0));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Criar_ComoAdmin_RetornaCreatedComId()
    {
        var categoriaId = await CriarCategoriaAsync();
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var resp = await admin.PostAsJsonAsync("/api/v1/produtos", CreateProdutoRequestFixture.Simples(categoriaId));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }
}
