using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Permissions;

/// <summary>
/// Testes end-to-end do CRUD de permissions (Task 12). Protegido por
/// <c>permissoes:gerenciar</c>, que o admin de teste possui. Cobre criação,
/// listagem, get e delete; cliente sem permissão recebe 403.
/// </summary>
[Collection("Integration")]
public class PermissionsEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public PermissionsEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CriarPermissao_Admin_RetornaCreated_ListaContem_EDeleta()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var codigo = $"teste:{Guid.NewGuid():N}".Substring(0, 16);

        var createResp = await admin.PostAsJsonAsync("/api/v1/permissions",
            new { codigo, descricao = "permissão de teste", recurso = "teste", acao = "acao" });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var id = createDoc.RootElement.GetProperty("id").GetInt32();
        id.Should().BeGreaterThan(0);

        var listResp = await admin.GetAsync("/api/v1/permissions");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        listDoc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("codigo").GetString())
            .Should().Contain(codigo);

        var getResp = await admin.GetAsync($"/api/v1/permissions/{id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        getDoc.RootElement.GetProperty("codigo").GetString().Should().Be(codigo);

        var deleteResp = await admin.DeleteAsync($"/api/v1/permissions/{id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getAfter = await admin.GetAsync($"/api/v1/permissions/{id}");
        getAfter.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListarPermissoes_Cliente_RetornaForbidden()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.GetAsync("/api/v1/permissions");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
