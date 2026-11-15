using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V2.Pagamentos;

[Collection("Integration")]
public class PagamentosV2EndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public PagamentosV2EndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PostPixV2_SemToken_401()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v2/pagamentos/pix", new { pedidoId = Guid.NewGuid() });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostPixV2_ComPedidoValido_201_ComQrEOperadoraMercadoPagoOrders()
    {
        var (cliente, pedidoId, total) = await CriarPedidoViaHttpAsync();

        var resp = await cliente.PostAsJsonAsync("/api/v2/pagamentos/pix", new { pedidoId, valor = total });

        var body = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.Created, body);
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("operadora").GetString().Should().Be("mercadopago-orders");
        doc.RootElement.GetProperty("pixCopiaCola").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetStatusV2_Retorna200()
    {
        var (cliente, pedidoId, total) = await CriarPedidoViaHttpAsync();
        var iniciar = await cliente.PostAsJsonAsync("/api/v2/pagamentos/pix", new { pedidoId, valor = total });
        iniciar.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid transacaoId;
        using (var doc = JsonDocument.Parse(await iniciar.Content.ReadAsStringAsync()))
            transacaoId = doc.RootElement.GetProperty("id").GetGuid();

        var pix = await cliente.GetAsync($"/api/v2/pagamentos/pix/{transacaoId}/status");
        var generica = await cliente.GetAsync($"/api/v2/pagamentos/{transacaoId}/status");

        pix.StatusCode.Should().Be(HttpStatusCode.OK);
        generica.StatusCode.Should().Be(HttpStatusCode.OK);
        using var sDoc = JsonDocument.Parse(await pix.Content.ReadAsStringAsync());
        sDoc.RootElement.GetProperty("id").GetGuid().Should().Be(transacaoId);
    }

    [Fact]
    public async Task Webhook_SemAssinatura_401()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync(
            "/api/v2/pagamentos/webhook/mercadopago?data.id=ORD01&type=order",
            new { type = "order", data = new { id = "ORD01" } });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhook_TsExtremo_401()
    {
        // Com segredo configurado, ts extremo precisa ser rejeitado (401) sem lançar (500).
        using var factory = _factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?> { ["MercadoPago:WebhookSecret"] = "segredo-falso-de-teste" })));
        var client = factory.CreateClient();

        foreach (var ts in new[] { long.MinValue.ToString(), long.MaxValue.ToString(), "99999999999999999999" })
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/pagamentos/webhook/mercadopago?data.id=ORD01&type=order");
            req.Headers.Add("x-signature", $"ts={ts},v1=abcdef");
            req.Headers.Add("x-request-id", "req-1");

            var resp = await client.SendAsync(req);

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"ts={ts}");
        }
    }

    [Fact]
    public async Task GetOpcoesV2_Retorna200()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.GetAsync("/api/v2/pagamentos/opcoes");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task RotasV1_ContinuamResponding()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.GetAsync("/api/v1/pagamentos/opcoes");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
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

        // O checkout valida estoque: repõe antes de adicionar ao carrinho.
        var estoque = await admin.PutAsJsonAsync($"/api/v1/produtos/{produtoId}/estoque",
            new { tipo = 0, quantidade = 100, observacao = "teste" });
        estoque.IsSuccessStatusCode.Should().BeTrue(await estoque.Content.ReadAsStringAsync());

        var (cliente, _) = await _factory.CreateClienteClientAsync();
        var add = await cliente.PostAsJsonAsync("/api/v1/carrinho/itens", new { produtoId, quantidade });
        add.IsSuccessStatusCode.Should().BeTrue(await add.Content.ReadAsStringAsync());

        var checkout = await cliente.PostAsJsonAsync("/api/v1/pedidos/checkout", new { clienteNome = "Cliente Teste" });
        var checkoutBody = await checkout.Content.ReadAsStringAsync();
        checkout.StatusCode.Should().Be(HttpStatusCode.Created, checkoutBody);

        using var doc = JsonDocument.Parse(checkoutBody);
        var pedidoId = doc.RootElement.GetProperty("pedidoId").GetGuid();
        return (cliente, pedidoId, preco * quantidade);
    }
}
