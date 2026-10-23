using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;
using Xunit.Abstractions;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Pagamentos.ValidarPagamentoParaAprovacao;

[Collection("Integration")]
public class ValidarPagamentoParaAprovacaoEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public ValidarPagamentoParaAprovacaoEndpointTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Validar_ComoAdmin_TransacaoAprovada_RetornaOk()
    {
        var (cliente, pedidoId, total) = await CriarPedidoViaHttpAsync();

        // Inicia e confirma o Pix (mock aprova), capturando a transacaoId.
        var iniciar = await cliente.PostAsJsonAsync("/api/v1/pagamentos/iniciar",
            new { pedidoId, metodo = 2, valor = total });
        Guid transacaoId;
        using (var doc = JsonDocument.Parse(await iniciar.Content.ReadAsStringAsync()))
            transacaoId = doc.RootElement.GetProperty("id").GetGuid();
        await cliente.PostAsync($"/api/v1/pagamentos/{transacaoId}/pix/confirmar", null);

        var (admin, _) = await _factory.CreateAdminClientAsync();
        var resp = await admin.PostAsync($"/api/v1/pagamentos/{transacaoId}/validar", null);

        resp.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Validar_ComoCliente_RetornaForbidden()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.PostAsync($"/api/v1/pagamentos/{Guid.NewGuid()}/validar", null);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Validar_TransacaoNaoAprovada_RetornaBadRequest()
    {
        var (cliente, pedidoId, total) = await CriarPedidoViaHttpAsync();

        // Inicia mas NÃO confirma -> permanece Pendente.
        var iniciar = await cliente.PostAsJsonAsync("/api/v1/pagamentos/iniciar",
            new { pedidoId, metodo = 2, valor = total });
        Guid transacaoId;
        using (var doc = JsonDocument.Parse(await iniciar.Content.ReadAsStringAsync()))
            transacaoId = doc.RootElement.GetProperty("id").GetGuid();

        var (admin, _) = await _factory.CreateAdminClientAsync();
        var resp = await admin.PostAsync($"/api/v1/pagamentos/{transacaoId}/validar", null);

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
