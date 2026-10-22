using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;
using Xunit.Abstractions;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Pagamentos.ProcessarPagamentoCartao;

[Collection("Integration")]
public class ProcessarPagamentoCartaoEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public ProcessarPagamentoCartaoEndpointTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Cartao_TransacaoPendente_AprovaPagamento()
    {
        var (cliente, pedidoId, total) = await CriarPedidoViaHttpAsync();

        // Inicia pagamento de cartão (metodo = 1 CartaoCredito) e captura a transacaoId.
        var iniciar = await cliente.PostAsJsonAsync("/api/v1/pagamentos/iniciar",
            new { pedidoId, metodo = 1, valor = total });
        Guid transacaoId;
        using (var doc = JsonDocument.Parse(await iniciar.Content.ReadAsStringAsync()))
            transacaoId = doc.RootElement.GetProperty("id").GetGuid();

        var cartao = await cliente.PostAsJsonAsync($"/api/v1/pagamentos/{transacaoId}/cartao",
            new { numero = "4111111111111111", nome = "CESAR BOTAS", validade = "12/30", cvv = "123" });

        cartao.IsSuccessStatusCode.Should().BeTrue();
        using var cDoc = JsonDocument.Parse(await cartao.Content.ReadAsStringAsync());
        cDoc.RootElement.GetProperty("status").GetString().Should().Be("aprovado");
        cDoc.RootElement.GetProperty("cartaoUltimos4").GetString().Should().Be("1111");
    }

    [Fact]
    public async Task Cartao_TransacaoInexistente_RetornaBadRequest()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.PostAsJsonAsync($"/api/v1/pagamentos/{Guid.NewGuid()}/cartao",
            new { numero = "4111111111111111", nome = "X", validade = "12/30", cvv = "123" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
