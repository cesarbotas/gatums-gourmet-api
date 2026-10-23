using System.Net;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Carrinho.GetCarrinho;

[Collection("Integration")]
public class GetCarrinhoEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public GetCarrinhoEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_SemAutenticacao_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/carrinho");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_CarrinhoVazio_RetornaTotalZero()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.GetAsync("/api/v1/carrinho");

        resp.IsSuccessStatusCode.Should().BeTrue();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("total").GetDecimal().Should().Be(0m);
        doc.RootElement.GetProperty("itens").GetArrayLength().Should().Be(0);
    }
}
