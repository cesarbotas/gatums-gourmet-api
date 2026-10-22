using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Categorias.ListCategorias;

[Collection("Integration")]
public class ListCategoriasEndpointTests
{
    private readonly HttpClient _client;

    public ListCategoriasEndpointTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Categorias_Publico_RetornaPaginado()
    {
        var resp = await _client.GetAsync("/api/v1/categorias?pagina=1&tamanho=10");

        resp.IsSuccessStatusCode.Should().BeTrue();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.TryGetProperty("itens", out _).Should().BeTrue();
        root.TryGetProperty("paginaAtual", out _).Should().BeTrue();
        root.TryGetProperty("tamanhoPagina", out _).Should().BeTrue();
        root.TryGetProperty("totalRegistros", out _).Should().BeTrue();
        root.TryGetProperty("totalPaginas", out _).Should().BeTrue();
        root.TryGetProperty("temPaginaAnterior", out _).Should().BeTrue();
        root.TryGetProperty("temProximaPagina", out _).Should().BeTrue();
    }
}
