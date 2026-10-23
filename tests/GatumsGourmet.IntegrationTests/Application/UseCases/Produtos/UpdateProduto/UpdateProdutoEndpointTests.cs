using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Produtos.UpdateProduto;

[Collection("Integration")]
public class UpdateProdutoEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public UpdateProdutoEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static object UpdateBody(long categoriaId) => new
    {
        nome = "Pizza Atualizada",
        descricao = "nova desc",
        categoriaId,
        preco = 59.90m,
        fotoUrl = "http://img/nova.png",
        disponivel = false
    };

    private async Task<long> CriarCategoriaAsync(HttpClient admin)
    {
        var catResp = await admin.PostAsJsonAsync("/api/v1/categorias",
            new { nome = $"Cat {Guid.NewGuid():N}", descricao = "d", ativo = true });
        using var catDoc = JsonDocument.Parse(await catResp.Content.ReadAsStringAsync());
        return catDoc.RootElement.GetProperty("id").GetInt64();
    }

    [Fact]
    public async Task Update_ComoAdmin_ProdutoExistente_RetornaOk()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var categoriaId = await CriarCategoriaAsync(admin);

        // Cria o produto via POST e captura o Id para atualizar em seguida.
        var create = await admin.PostAsJsonAsync("/api/v1/produtos", new
        {
            nome = $"Produto {Guid.NewGuid():N}",
            descricao = "d",
            categoriaId,
            preco = 20m,
            fotoUrl = "http://img/x.png",
            disponivel = true,
            tipo = 0,
            itens = (object?)null
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var resp = await admin.PutAsJsonAsync($"/api/v1/produtos/{id}", UpdateBody(categoriaId));

        resp.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ProdutoInexistente_RetornaBadRequest()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var categoriaId = await CriarCategoriaAsync(admin);

        var resp = await admin.PutAsJsonAsync($"/api/v1/produtos/{Guid.NewGuid()}", UpdateBody(categoriaId));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
