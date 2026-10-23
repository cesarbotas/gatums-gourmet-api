using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Produtos.DeleteProduto;

[Collection("Integration")]
public class DeleteProdutoEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DeleteProdutoEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Delete_ComoAdmin_RemoveProduto()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var catResp = await admin.PostAsJsonAsync("/api/v1/categorias",
            new { nome = $"Cat {Guid.NewGuid():N}", descricao = "d", ativo = true });
        using var catDoc = JsonDocument.Parse(await catResp.Content.ReadAsStringAsync());
        var categoriaId = catDoc.RootElement.GetProperty("id").GetInt64();

        // Cria o produto via POST e captura o Id para removê-lo em seguida.
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

        var delete = await admin.DeleteAsync($"/api/v1/produtos/{id}");
        delete.IsSuccessStatusCode.Should().BeTrue();

        // Confirma remoção: GET retorna 400 (Result.Fail -> Problem 400)
        var getAfter = await _client.GetAsync($"/api/v1/produtos/{id}");
        getAfter.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_Inexistente_RetornaBadRequest()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var resp = await admin.DeleteAsync($"/api/v1/produtos/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
