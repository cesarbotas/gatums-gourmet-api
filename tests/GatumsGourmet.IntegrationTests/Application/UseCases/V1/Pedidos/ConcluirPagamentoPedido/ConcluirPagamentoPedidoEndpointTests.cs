using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;
using Xunit.Abstractions;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Pedidos.ConcluirPagamentoPedido;

[Collection("Integration")]
public class ConcluirPagamentoPedidoEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public ConcluirPagamentoPedidoEndpointTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Concluir_ComTransacaoAprovada_AvancaParaPagamento()
    {
        var (cliente, pedidoId, total) = await CriarPedidoViaHttpAsync();

        // Inicia pagamento Pix (metodo = 2) e captura a transacaoId do JSON.
        var iniciar = await cliente.PostAsJsonAsync("/api/v1/pagamentos/iniciar",
            new { pedidoId, metodo = 2, valor = total });
        iniciar.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid transacaoId;
        using (var doc = JsonDocument.Parse(await iniciar.Content.ReadAsStringAsync()))
            transacaoId = doc.RootElement.GetProperty("id").GetGuid();

        // Confirma o Pix (mock aprova)
        var confirmar = await cliente.PostAsync($"/api/v1/pagamentos/{transacaoId}/pix/confirmar", null);
        confirmar.IsSuccessStatusCode.Should().BeTrue();

        // Conclui o pagamento no pedido
        var concluir = await cliente.PostAsJsonAsync($"/api/v1/pedidos/{pedidoId}/pagamento/concluir",
            new { transacaoId });
        concluir.IsSuccessStatusCode.Should().BeTrue();

        var get = await cliente.GetAsync($"/api/v1/pedidos/{pedidoId}");
        using (var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("statusAtual").GetString().Should().Be("pagamento");
    }

    [Fact]
    public async Task Concluir_TransacaoInexistente_RetornaBadRequest()
    {
        var (cliente, pedidoId, _) = await CriarPedidoViaHttpAsync();

        var concluir = await cliente.PostAsJsonAsync($"/api/v1/pedidos/{pedidoId}/pagamento/concluir",
            new { transacaoId = Guid.NewGuid() });

        concluir.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
