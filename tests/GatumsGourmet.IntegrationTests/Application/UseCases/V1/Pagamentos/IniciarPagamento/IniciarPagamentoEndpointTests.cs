using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;
using Xunit.Abstractions;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Pagamentos.IniciarPagamento;

[Collection("Integration")]
public class IniciarPagamentoEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public IniciarPagamentoEndpointTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Iniciar_Pix_RetornaCreatedComCopiaCola()
    {
        var (cliente, pedidoId, total) = await CriarPedidoViaHttpAsync();

        var resp = await cliente.PostAsJsonAsync("/api/v1/pagamentos/iniciar",
            new { pedidoId, metodo = 2, valor = total });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("status").GetString().Should().Be("pendente");
        doc.RootElement.GetProperty("pixCopiaCola").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Iniciar_PedidoInexistente_RetornaBadRequest()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.PostAsJsonAsync("/api/v1/pagamentos/iniciar",
            new { pedidoId = Guid.NewGuid(), metodo = 2, valor = 10m });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Iniciar_ChamadaDuplicada_RetornaMesmaTransacao()
    {
        var (cliente, pedidoId, total) = await CriarPedidoViaHttpAsync();

        var r1 = await cliente.PostAsJsonAsync("/api/v1/pagamentos/iniciar", new { pedidoId, metodo = 2, valor = total });
        var r2 = await cliente.PostAsJsonAsync("/api/v1/pagamentos/iniciar", new { pedidoId, metodo = 2, valor = total });

        Guid id1, id2;
        using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync())) id1 = d1.RootElement.GetProperty("id").GetGuid();
        using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync())) id2 = d2.RootElement.GetProperty("id").GetGuid();

        id2.Should().Be(id1); // idempotência por transação pendente
    }

    private async Task<(HttpClient Cliente, Guid PedidoId, decimal Total)> CriarPedidoViaHttpAsync()
    {
        const decimal preco = 30m;
        const int quantidade = 2;

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
            preco,
            fotoUrl = "http://img/x.png",
            disponivel = true,
            tipo = 0,
            itens = (object?)null
        });
        using var prodDoc = JsonDocument.Parse(await createProduto.Content.ReadAsStringAsync());
        var produtoId = prodDoc.RootElement.GetProperty("id").GetGuid();

        var (cliente, _) = await _factory.CreateClienteClientAsync();
        await cliente.PostAsJsonAsync("/api/v1/carrinho/itens", new { produtoId, quantidade });

        var checkout = await cliente.PostAsJsonAsync("/api/v1/pedidos/checkout", new { clienteNome = "Cliente Teste" });
        var checkoutBody = await checkout.Content.ReadAsStringAsync();
        _output.WriteLine($"Checkout body: {checkoutBody}");
        checkout.StatusCode.Should().Be(HttpStatusCode.Created);

        using var doc = JsonDocument.Parse(checkoutBody);
        var pedidoId = doc.RootElement.GetProperty("pedidoId").GetGuid();
        return (cliente, pedidoId, preco * quantidade);
    }
}
