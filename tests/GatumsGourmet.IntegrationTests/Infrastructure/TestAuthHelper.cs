using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GatumsGourmet.IntegrationTests.Infrastructure;

/// <summary>
/// Helper para autenticar nos testes de endpoint HTTP.
/// Obtém tokens JWT via login/registro e cria HttpClients autenticados.
/// </summary>
public static class TestAuthHelper
{
    // ── Tokens ──────────────────────────────────────────

    /// <summary>
    /// Faz login como admin (seed) e retorna o token JWT.
    /// </summary>
    public static async Task<string> GetAdminTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = IntegrationTestFixture.AdminEmail,
            senha = IntegrationTestFixture.AdminPassword
        });

        response.EnsureSuccessStatusCode();
        return await ExtractTokenAsync(response);
    }

    /// <summary>
    /// Registra um cliente de teste, faz login e retorna (token, userId).
    /// Usa email único por chamada para evitar conflitos.
    /// </summary>
    public static async Task<(string Token, Guid UserId)> RegisterAndLoginClienteAsync(
        HttpClient client,
        string? email = null,
        string senha = "Cliente@123")
    {
        email ??= $"cliente-{Guid.NewGuid():N}@teste.com";

        // Registrar
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            nome = "Cliente Teste",
            email,
            senha,
            telefone = (string?)null
        });
        registerResponse.EnsureSuccessStatusCode();

        // Login
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            senha
        });
        loginResponse.EnsureSuccessStatusCode();

        var json = await loginResponse.Content.ReadFromJsonAsync<JsonDocument>()!;
        var token = json.RootElement.GetProperty("token").GetString()!;
        var userId = Guid.Parse(json.RootElement
            .GetProperty("user").GetProperty("id").GetString()!);

        return (token, userId);
    }

    // ── Clients autenticados ────────────────────────────

    /// <summary>
    /// Cria um HttpClient autenticado como admin.
    /// </summary>
    public static async Task<HttpClient> CreateAdminClientAsync(IntegrationWebAppFactory factory)
    {
        var client = factory.CreateClient();
        var token = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Cria um HttpClient autenticado como cliente.
    /// </summary>
    public static async Task<HttpClient> CreateClienteClientAsync(
        IntegrationWebAppFactory factory,
        string? email = null)
    {
        var client = factory.CreateClient();
        var (token, _) = await RegisterAndLoginClienteAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ── Util ─────────────────────────────────────────────

    private static async Task<string> ExtractTokenAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>()!;
        return json.RootElement.GetProperty("token").GetString()!;
    }
}
