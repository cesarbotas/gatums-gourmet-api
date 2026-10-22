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
    public async Task Categorias_Publico_RetornaOk()
    {
        var resp = await _client.GetAsync("/api/v1/categorias");

        resp.IsSuccessStatusCode.Should().BeTrue();
    }
}
