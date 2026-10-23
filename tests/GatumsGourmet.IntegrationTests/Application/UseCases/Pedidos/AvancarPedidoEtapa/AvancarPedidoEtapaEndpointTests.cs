using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;
using Xunit.Abstractions;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Pedidos.AvancarPedidoEtapa;

[Collection("Integration")]
public class AvancarPedidoEtapaEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public AvancarPedidoEtapaEndpointTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Avancar_CriadoParaAprovado_ComoAdmin_FunctionaOk()
    {
        var pedidoId = await CriarPedidoViaHttpAsync();
        var (admin, _) = await _factory.CreateAdminClientAsync();

        // Criado -> Aprovado (ProximaEtapa = 1)
        var resp = await admin.PostAsJsonAsync($"/api/v1/pedidos/{pedidoId}/etapas/avancar",
            new { proximaEtapa = 1, observacao = "aprovado pelo admin", entregaTipo = (int?)null });

        resp.IsSuccessStatusCode.Should().BeTrue();

        var get = await admin.GetAsync($"/api/v1/pedidos/{pedidoId}");
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("statusAtual").GetString().Should().Be("aprovado");
    }

    [Fact]
    public async Task Avancar_TransicaoInvalida_RetornaBadRequest()
    {
        var pedidoId = await CriarPedidoViaHttpAsync();
        var (admin, _) = await _factory.CreateAdminClientAsync();

        // Criado -> Finalizado (6) não é permitido -> BusinessRuleException ->
        // ExceptionMiddleware mapeia para 422 Unprocessable Entity.
        var resp = await admin.PostAsJsonAsync($"/api/v1/pedidos/{pedidoId}/etapas/avancar",
            new { proximaEtapa = 6, observacao = (string?)null, entregaTipo = (int?)null });

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private async Task<Guid> CriarPedidoViaHttpAsync()
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
        return doc.RootElement.GetProperty("pedidoId").GetGuid();
    }
}
