using FluentAssertions;
using System.Text.Json;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Produtos.ListProdutos;

[Collection("Integration")]
public class ListProdutosEndpointTests
{
    private readonly HttpClient _client;

    public ListProdutosEndpointTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Listar_Publico_RetornaPaginado()
    {
        var resp = await _client.GetAsync("/api/v1/produtos?pagina=1&tamanho=10");

        resp.IsSuccessStatusCode.Should().BeTrue();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.TryGetProperty("itens", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("totalRegistros", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("totalPaginas", out _).Should().BeTrue();
    }
}
