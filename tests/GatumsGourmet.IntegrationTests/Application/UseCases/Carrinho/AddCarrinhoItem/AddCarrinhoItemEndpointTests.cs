using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Carrinho.AddCarrinhoItem;

[Collection("Integration")]
public class AddCarrinhoItemEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AddCarrinhoItemEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    // Cria um produto via POST (admin) e devolve o Id, para usar no carrinho.
    private async Task<Guid> CriarProdutoAsync(decimal preco)
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var create = await admin.PostAsJsonAsync("/api/v1/produtos", new
        {
            nome = $"Item {Guid.NewGuid():N}",
            descricao = "d",
            categoria = "Lanches",
            preco,
            fotoUrl = "http://img/x.png",
            disponivel = true,
            tipo = 0,
            itens = (object?)null
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Add_SemAutenticacao_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/carrinho/itens",
            new { produtoId = Guid.NewGuid(), quantidade = 1 });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Add_ProdutoValido_AdicionaECalculaTotal()
    {
        var produtoId = await CriarProdutoAsync(preco: 20m);
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var add = await cliente.PostAsJsonAsync("/api/v1/carrinho/itens", new { produtoId, quantidade = 2 });
        add.IsSuccessStatusCode.Should().BeTrue();

        var get = await cliente.GetAsync("/api/v1/carrinho");
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("total").GetDecimal().Should().Be(40m);
        doc.RootElement.GetProperty("itens").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Add_ProdutoInexistente_RetornaBadRequest()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.PostAsJsonAsync("/api/v1/carrinho/itens",
            new { produtoId = Guid.NewGuid(), quantidade = 1 });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
