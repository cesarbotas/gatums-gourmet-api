using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Carrinho.RemoveCarrinhoItem;

[Collection("Integration")]
public class RemoveCarrinhoItemEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public RemoveCarrinhoItemEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

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
    public async Task Delete_ItemExistente_RemoveDoCarrinho()
    {
        var produtoId = await CriarProdutoAsync(preco: 15m);
        var (cliente, _) = await _factory.CreateClienteClientAsync();
        await cliente.PostAsJsonAsync("/api/v1/carrinho/itens", new { produtoId, quantidade = 1 });

        var remove = await cliente.DeleteAsync($"/api/v1/carrinho/itens/{produtoId}");
        remove.IsSuccessStatusCode.Should().BeTrue();

        var get = await cliente.GetAsync("/api/v1/carrinho");
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("itens").GetArrayLength().Should().Be(0);
    }
}
