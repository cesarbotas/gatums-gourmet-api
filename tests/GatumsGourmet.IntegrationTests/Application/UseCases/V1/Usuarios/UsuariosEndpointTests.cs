using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Usuarios;

/// <summary>
/// Testes end-to-end do CRUD de usuários (Task 13). O admin de teste possui a
/// permissão <c>usuarios:gerenciar</c>. Cobre o happy path: criar usuário com
/// role, trocar roles, ativar/desativar, soft-delete (preserva o registro) e o
/// bloqueio 403 para cliente. A regra "nunca sem admin" é coberta de forma
/// determinística por testes unitários (UltimoAdminRuleTests), já que o banco
/// de teste pode conter vários admins ativos.
/// </summary>
[Collection("Integration")]
public class UsuariosEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public UsuariosEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static async Task<int> GetRoleIdAsync(HttpClient admin, string nome)
    {
        var resp = await admin.GetAsync("/api/v1/roles");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.EnumerateArray()
            .First(e => e.GetProperty("nome").GetString() == nome)
            .GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task CriarUsuario_ComRoleGerente_RetornaCreated_EGetConfirma()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var gerenteId = await GetRoleIdAsync(admin, "Gerente");

        var email = $"u-{Guid.NewGuid():N}@gatums.com";
        var createResp = await admin.PostAsJsonAsync("/api/v1/usuarios", new
        {
            nome = "Usuário Teste",
            email,
            senha = "Senha@123",
            telefone = "11999999999",
            roleIds = new[] { gerenteId }
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var id = createDoc.RootElement.GetProperty("id").GetGuid();

        var getResp = await admin.GetAsync($"/api/v1/usuarios/{id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        getDoc.RootElement.GetProperty("ativo").GetBoolean().Should().BeTrue();
        getDoc.RootElement.GetProperty("email").GetString().Should().Be(email);
        getDoc.RootElement.GetProperty("roles").EnumerateArray()
            .Select(e => e.GetString()).Should().Contain("Gerente");
    }

    [Fact]
    public async Task SetRoles_TrocaDeRole_RetornaOk_EGetConfirma()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var gerenteId = await GetRoleIdAsync(admin, "Gerente");
        var clienteId = await GetRoleIdAsync(admin, "Cliente");

        var createResp = await admin.PostAsJsonAsync("/api/v1/usuarios", new
        {
            nome = "Troca Role",
            email = $"u-{Guid.NewGuid():N}@gatums.com",
            senha = "Senha@123",
            roleIds = new[] { gerenteId }
        });
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetGuid();

        var setResp = await admin.PutAsJsonAsync($"/api/v1/usuarios/{id}/roles",
            new { roleIds = new[] { clienteId } });
        setResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await admin.GetAsync($"/api/v1/usuarios/{id}");
        using var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        getDoc.RootElement.GetProperty("roles").EnumerateArray()
            .Select(e => e.GetString()).Should().BeEquivalentTo(new[] { "Cliente" });
    }

    [Fact]
    public async Task SetAtivo_Desativa_RetornaOk_EGetConfirma()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var gerenteId = await GetRoleIdAsync(admin, "Gerente");

        var createResp = await admin.PostAsJsonAsync("/api/v1/usuarios", new
        {
            nome = "Desativa",
            email = $"u-{Guid.NewGuid():N}@gatums.com",
            senha = "Senha@123",
            roleIds = new[] { gerenteId }
        });
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetGuid();

        var patchResp = await admin.PatchAsJsonAsync($"/api/v1/usuarios/{id}/ativo", new { ativo = false });
        patchResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await admin.GetAsync($"/api/v1/usuarios/{id}");
        using var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        getDoc.RootElement.GetProperty("ativo").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Delete_SoftDelete_UsuarioPermaneceComAtivoFalse()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();
        var gerenteId = await GetRoleIdAsync(admin, "Gerente");

        var createResp = await admin.PostAsJsonAsync("/api/v1/usuarios", new
        {
            nome = "Soft Delete",
            email = $"u-{Guid.NewGuid():N}@gatums.com",
            senha = "Senha@123",
            roleIds = new[] { gerenteId }
        });
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetGuid();

        var deleteResp = await admin.DeleteAsync($"/api/v1/usuarios/{id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Soft-delete: o registro NÃO some, apenas Ativo=false.
        var getResp = await admin.GetAsync($"/api/v1/usuarios/{id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        getDoc.RootElement.GetProperty("ativo").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ListarUsuarios_Cliente_RetornaForbidden()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.GetAsync("/api/v1/usuarios");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
