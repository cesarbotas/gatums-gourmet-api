using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;
using Xunit.Abstractions;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Pedidos.GetPedidoById;

[Collection("Integration")]
public class GetPedidoByIdEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public GetPedidoByIdEndpointTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Get_DonoDoPedido_RetornaPedido()
    {
        var (cliente, pedidoId) = await CriarPedidoViaHttpAsync();

        var resp = await cliente.GetAsync($"/api/v1/pedidos/{pedidoId}");

        resp.IsSuccessStatusCode.Should().BeTrue();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("id").GetGuid().Should().Be(pedidoId);
        doc.RootElement.GetProperty("statusAtual").GetString().Should().Be("criado");
    }

    [Fact]
    public async Task Get_OutroCliente_RetornaAcessoNegado()
    {
        var (_, pedidoId) = await CriarPedidoViaHttpAsync();
        var (outroCliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await outroCliente.GetAsync($"/api/v1/pedidos/{pedidoId}");

        // Handler retorna Result.Fail("Acesso negado.") -> Problem 400
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_Admin_AcessaPedidoDeOutro()
    {
        var (_, pedidoId) = await CriarPedidoViaHttpAsync();
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var resp = await admin.GetAsync($"/api/v1/pedidos/{pedidoId}");

        resp.IsSuccessStatusCode.Should().BeTrue();
    }

    // Monta o cenário via HTTP: cria produto (POST admin), adiciona ao carrinho (POST cliente)
    // e faz checkout (POST cliente), extraindo o pedidoId do JSON retornado.
    private async Task<(HttpClient Cliente, Guid PedidoId)> CriarPedidoViaHttpAsync()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var catResp = await admin.PostAsJsonAsync("/api/v1/categorias",
            new { nome = $"Cat {Guid.NewGuid():N}", descricao = "d", ativo = true });
        using var catDoc = JsonDocument.Parse(await catResp.Content.ReadAsStringAsync());
        var categoriaId = catDoc.RootElement.GetProperty("id").GetInt64();

        var createProduto = await admin.PostAsJsonAsync("/api/v1/produtos", new
        {
            nome = $"Item {Guid.NewGuid():N}",
            descricao = "d",
            categoriaId,
            preco = 30m,
            fotoUrl = "http://img/x.png",
            disponivel = true,
            tipo = 0,
            itens = (object?)null
        });
        using var prodDoc = JsonDocument.Parse(await createProduto.Content.ReadAsStringAsync());
        var produtoId = prodDoc.RootElement.GetProperty("id").GetGuid();

        var (cliente, _) = await _factory.CreateClienteClientAsync();
        await cliente.PostAsJsonAsync("/api/v1/carrinho/itens", new { produtoId, quantidade = 2 });

        var checkout = await cliente.PostAsJsonAsync("/api/v1/pedidos/checkout", new { clienteNome = "Cliente Teste" });
        var checkoutBody = await checkout.Content.ReadAsStringAsync();
        _output.WriteLine($"Checkout body: {checkoutBody}");
        checkout.StatusCode.Should().Be(HttpStatusCode.Created);

        using var doc = JsonDocument.Parse(checkoutBody);
        var pedidoId = doc.RootElement.GetProperty("pedidoId").GetGuid();
        return (cliente, pedidoId);
    }
}
