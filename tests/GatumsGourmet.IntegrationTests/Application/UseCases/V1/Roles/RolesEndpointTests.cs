using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Roles;

/// <summary>
/// Testes end-to-end do CRUD de roles (Task 12). O admin de teste possui a
/// permissão <c>roles:gerenciar</c> (todas as 12 do seed). Cobre criação,
/// listagem, get, vínculo de permissões, delete e o bloqueio de roles de
/// sistema (Admin/Cliente). Cliente (sem a permissão) recebe 403.
/// </summary>
[Collection("Integration")]
public class RolesEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public RolesEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CriarRole_Admin_RetornaCreated_ListaContem_EGetConfirma()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var nome = $"Role {Guid.NewGuid():N}";

        var createResp = await admin.PostAsJsonAsync("/api/v1/roles", new { nome, descricao = "desc" });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var id = createDoc.RootElement.GetProperty("id").GetInt32();
        id.Should().BeGreaterThan(0);

        var listResp = await admin.GetAsync("/api/v1/roles");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        listDoc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("nome").GetString())
            .Should().Contain(nome);

        var getResp = await admin.GetAsync($"/api/v1/roles/{id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        getDoc.RootElement.GetProperty("nome").GetString().Should().Be(nome);
        getDoc.RootElement.GetProperty("sistema").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task SetPermissions_VinculaDuasPermissoes_GetConfirma()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var createResp = await admin.PostAsJsonAsync("/api/v1/roles",
            new { nome = $"Role {Guid.NewGuid():N}", descricao = (string?)null });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var roleId = createDoc.RootElement.GetProperty("id").GetInt32();

        var permsResp = await admin.GetAsync("/api/v1/permissions");
        permsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var permsDoc = JsonDocument.Parse(await permsResp.Content.ReadAsStringAsync());
        var perms = permsDoc.RootElement.EnumerateArray()
            .Select(e => new { Id = e.GetProperty("id").GetInt32(), Codigo = e.GetProperty("codigo").GetString()! })
            .Take(2)
            .ToList();
        perms.Should().HaveCount(2);

        var setResp = await admin.PutAsJsonAsync($"/api/v1/roles/{roleId}/permissions",
            new { permissionIds = perms.Select(p => p.Id).ToArray() });
        setResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await admin.GetAsync($"/api/v1/roles/{roleId}");
        using var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        var codigos = getDoc.RootElement.GetProperty("permissoes").EnumerateArray()
            .Select(e => e.GetString()).ToList();
        codigos.Should().BeEquivalentTo(perms.Select(p => p.Codigo));
    }

    [Fact]
    public async Task DeleteRole_DeSistema_RetornaBadRequest()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var listResp = await admin.GetAsync("/api/v1/roles");
        using var listDoc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        var adminRoleId = listDoc.RootElement.EnumerateArray()
            .First(e => e.GetProperty("nome").GetString() == "Admin")
            .GetProperty("id").GetInt32();

        var deleteResp = await admin.DeleteAsync($"/api/v1/roles/{adminRoleId}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPermissions_RoleDeSistema_RetornaBadRequest()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var listResp = await admin.GetAsync("/api/v1/roles");
        using var listDoc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        var adminRoleId = listDoc.RootElement.EnumerateArray()
            .First(e => e.GetProperty("nome").GetString() == "Admin")
            .GetProperty("id").GetInt32();

        var setResp = await admin.PutAsJsonAsync($"/api/v1/roles/{adminRoleId}/permissions",
            new { permissionIds = Array.Empty<int>() });
        setResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteRole_Vazia_RetornaOk()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var createResp = await admin.PostAsJsonAsync("/api/v1/roles",
            new { nome = $"Role {Guid.NewGuid():N}", descricao = "temporaria" });
        using var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var roleId = createDoc.RootElement.GetProperty("id").GetInt32();

        var deleteResp = await admin.DeleteAsync($"/api/v1/roles/{roleId}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await admin.GetAsync($"/api/v1/roles/{roleId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CriarRole_Cliente_RetornaForbidden()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.PostAsJsonAsync("/api/v1/roles",
            new { nome = $"Role {Guid.NewGuid():N}", descricao = "x" });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
