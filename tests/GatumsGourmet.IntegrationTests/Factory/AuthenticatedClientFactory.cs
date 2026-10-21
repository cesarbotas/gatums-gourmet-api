using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Enums.Auth;
using GatumsGourmet.Domain.Interfaces;
using GatumsGourmet.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GatumsGourmet.IntegrationTests.Factory;

/// <summary>
/// Dados de um usuário autenticado criado para um teste de integração.
/// </summary>
public sealed record AuthenticatedUser(Guid Id, string Email, string Role, string Token);

/// <summary>
/// Helpers para criar HttpClients autenticados nos testes de integração.
/// Semeia um usuário real no banco e gera um token JWT válido com o mesmo
/// gerador usado pela API (IJwtTokenGenerator), respeitando o secret de teste.
/// </summary>
public static class AuthenticatedClientFactory
{
    public static Task<(HttpClient Client, AuthenticatedUser User)> CreateAdminClientAsync(this CustomWebApplicationFactory factory)
        => factory.CreateAuthenticatedClientAsync(UserRole.Admin);

    public static Task<(HttpClient Client, AuthenticatedUser User)> CreateClienteClientAsync(this CustomWebApplicationFactory factory)
        => factory.CreateAuthenticatedClientAsync(UserRole.Cliente);

    public static async Task<(HttpClient Client, AuthenticatedUser User)> CreateAuthenticatedClientAsync(
        this CustomWebApplicationFactory factory, UserRole role)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GatumsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var roleLower = role.ToString().ToLower();
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = $"Usuário {roleLower}",
            Email = $"{roleLower}-{Guid.NewGuid():N}@gatums.com",
            SenhaHash = hasher.Hash("senha123"),
            Role = role,
            CriadoEm = clock.UtcNow
        };

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        // Obtém o token pela própria API (login real), garantindo que o token é
        // assinado e validado com a mesma configuração usada em runtime.
        var client = factory.CreateClient();
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = usuario.Email, senha = "senha123" });

        if (!loginResp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Login de teste falhou ({(int)loginResp.StatusCode}) para {usuario.Email}: {await loginResp.Content.ReadAsStringAsync()}");

        using var doc = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
        var token = doc.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Token ausente na resposta de login.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, new AuthenticatedUser(usuario.Id, usuario.Email, roleLower, token));
    }
}
