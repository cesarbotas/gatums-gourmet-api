using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Carrinho.UpdateCarrinhoItemQuantidade;

[Collection("Integration")]
public class UpdateCarrinhoItemQuantidadeEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public UpdateCarrinhoItemQuantidadeEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

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
    public async Task Patch_AtualizaQuantidade()
    {
        var produtoId = await CriarProdutoAsync(preco: 10m);
        var (cliente, _) = await _factory.CreateClienteClientAsync();
        await cliente.PostAsJsonAsync("/api/v1/carrinho/itens", new { produtoId, quantidade = 1 });

        var patch = await cliente.PatchAsync($"/api/v1/carrinho/itens/{produtoId}",
            JsonContent.Create(new { quantidade = 4 }));
        patch.IsSuccessStatusCode.Should().BeTrue();

        var get = await cliente.GetAsync("/api/v1/carrinho");
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("total").GetDecimal().Should().Be(40m); // 10 * 4
    }

    [Fact]
    public async Task Patch_QuantidadeZero_RemoveItem()
    {
        var produtoId = await CriarProdutoAsync(preco: 10m);
        var (cliente, _) = await _factory.CreateClienteClientAsync();
        await cliente.PostAsJsonAsync("/api/v1/carrinho/itens", new { produtoId, quantidade = 2 });

        var patch = await cliente.PatchAsync($"/api/v1/carrinho/itens/{produtoId}",
            JsonContent.Create(new { quantidade = 0 }));
        patch.IsSuccessStatusCode.Should().BeTrue();

        var get = await cliente.GetAsync("/api/v1/carrinho");
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("itens").GetArrayLength().Should().Be(0);
    }
}
