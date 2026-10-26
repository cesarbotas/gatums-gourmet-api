using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Application.UseCases.V1.Produtos.CreateProduto;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Api.Authorization;

/// <summary>
/// Teste end-to-end da autorização por permissão (pointer da Task 10 / Task 11):
/// uma rota administrativa protegida por <c>RequirePermission("produtos:escrever")</c>
/// deve devolver 401 para anônimo, 403 para um usuário autenticado sem a permissão
/// (Cliente) e 201 para quem tem a permissão (Admin, com todas as 12 do seed).
/// </summary>
[Collection("Integration")]
public class PermissionEnforcementEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public PermissionEnforcementEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task RotaProtegidaPorPermissao_Anonimo_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/produtos", CreateProdutoRequestFixture.Simples(0));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RotaProtegidaPorPermissao_SemPermissao_RetornaForbidden()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.PostAsJsonAsync("/api/v1/produtos", CreateProdutoRequestFixture.Simples(0));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RotaProtegidaPorPermissao_ComPermissao_RetornaCreated()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        // Cria uma categoria válida (também protegida por permissão) e usa seu Id no produto.
        var catResp = await admin.PostAsJsonAsync("/api/v1/categorias",
            new { nome = $"Cat {Guid.NewGuid():N}", descricao = "d", ativo = true });
        catResp.StatusCode.Should().Be(HttpStatusCode.Created);
        using var catDoc = JsonDocument.Parse(await catResp.Content.ReadAsStringAsync());
        var categoriaId = catDoc.RootElement.GetProperty("id").GetInt64();

        var resp = await admin.PostAsJsonAsync("/api/v1/produtos", CreateProdutoRequestFixture.Simples(categoriaId));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
