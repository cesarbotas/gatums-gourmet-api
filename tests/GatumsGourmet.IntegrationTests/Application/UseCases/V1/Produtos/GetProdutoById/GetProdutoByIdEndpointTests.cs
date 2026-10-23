using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Produtos.GetProdutoById;

[Collection("Integration")]
public class GetProdutoByIdEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GetProdutoByIdEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ProdutoExistente_RetornaOk()
    {
        // Cria o produto via o endpoint POST (admin) e captura o Id retornado.
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var catResp = await admin.PostAsJsonAsync("/api/v1/categorias",
            new { nome = $"Cat {Guid.NewGuid():N}", descricao = "d", ativo = true });
        using var catDoc = JsonDocument.Parse(await catResp.Content.ReadAsStringAsync());
        var categoriaId = catDoc.RootElement.GetProperty("id").GetInt64();

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

        var resp = await _client.GetAsync($"/api/v1/produtos/{id}");

        resp.IsSuccessStatusCode.Should().BeTrue();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("id").GetGuid().Should().Be(id);
    }

    [Fact]
    public async Task Get_ProdutoInexistente_RetornaBadRequest()
    {
        var resp = await _client.GetAsync($"/api/v1/produtos/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
