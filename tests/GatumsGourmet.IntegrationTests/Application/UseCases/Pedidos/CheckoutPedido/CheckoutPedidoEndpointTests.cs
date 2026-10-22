using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Pedidos.CheckoutPedido;

[Collection("Integration")]
public class CheckoutPedidoEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public CheckoutPedidoEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    private async Task<Guid> CriarProdutoAsync(decimal preco)
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var catResp = await admin.PostAsJsonAsync("/api/v1/categorias",
            new { nome = $"Cat {Guid.NewGuid():N}", descricao = "d", ativo = true });
        using var catDoc = JsonDocument.Parse(await catResp.Content.ReadAsStringAsync());
        var categoriaId = catDoc.RootElement.GetProperty("id").GetInt64();

        var create = await admin.PostAsJsonAsync("/api/v1/produtos", new
        {
            nome = $"Item {Guid.NewGuid():N}",
            descricao = "d",
            categoriaId,
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
    public async Task Checkout_ComItens_CriaPedido()
    {
        var produtoId = await CriarProdutoAsync(preco: 30m);
        var (cliente, _) = await _factory.CreateClienteClientAsync();
        await cliente.PostAsJsonAsync("/api/v1/carrinho/itens", new { produtoId, quantidade = 2 });

        var checkout = await cliente.PostAsJsonAsync("/api/v1/pedidos/checkout", new { clienteNome = "César" });

        checkout.StatusCode.Should().Be(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await checkout.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("pedidoId").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task Checkout_CarrinhoVazio_RetornaBadRequest()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var checkout = await cliente.PostAsJsonAsync("/api/v1/pedidos/checkout", new { clienteNome = "Vazio" });

        checkout.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_SemAutenticacao_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var checkout = await client.PostAsJsonAsync("/api/v1/pedidos/checkout", new { clienteNome = "X" });

        checkout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
