using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;
using Xunit.Abstractions;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Pagamentos.ConsultarStatusPagamento;

[Collection("Integration")]
public class ConsultarStatusPagamentoEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public ConsultarStatusPagamentoEndpointTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Status_TransacaoExistente_RetornaOk()
    {
        var (cliente, pedidoId, total) = await CriarPedidoViaHttpAsync();

        // Inicia o pagamento via POST e captura a transacaoId para consultar no GET.
        var iniciar = await cliente.PostAsJsonAsync("/api/v1/pagamentos/iniciar",
            new { pedidoId, metodo = 2, valor = total });
        Guid transacaoId;
        using (var doc = JsonDocument.Parse(await iniciar.Content.ReadAsStringAsync()))
            transacaoId = doc.RootElement.GetProperty("id").GetGuid();

        var resp = await cliente.GetAsync($"/api/v1/pagamentos/{transacaoId}/status");

        resp.IsSuccessStatusCode.Should().BeTrue();
        using var sDoc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        sDoc.RootElement.GetProperty("id").GetGuid().Should().Be(transacaoId);
    }

    [Fact]
    public async Task Status_TransacaoInexistente_RetornaBadRequest()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.GetAsync($"/api/v1/pagamentos/{Guid.NewGuid()}/status");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<(HttpClient Cliente, Guid PedidoId, decimal Total)> CriarPedidoViaHttpAsync()
    {
        const decimal preco = 30m;
        const int quantidade = 2;

        var (admin, _) = await _factory.CreateAdminClientAsync();
        var createProduto = await admin.PostAsJsonAsync("/api/v1/produtos", new
        {
            nome = $"Item {Guid.NewGuid():N}",
            descricao = "d",
            categoria = "Combos",
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
