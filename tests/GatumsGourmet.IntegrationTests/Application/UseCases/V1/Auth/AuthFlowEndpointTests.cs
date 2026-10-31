using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Auth.RegisterUser;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Auth;

/// <summary>
/// Teste de integração do fluxo completo de autenticação com refresh token:
/// register → login → me → refresh → reúso (falha) → família revogada (falha) → logout.
///
/// O passo de reúso + família revogada confirma a detecção de reúso e a PERSISTÊNCIA
/// da revogação de família (quando um token já rotacionado é reapresentado, toda a
/// família — incluindo o refresh emitido na rotação — é revogada).
/// </summary>
[Collection("Integration")]
public class AuthFlowEndpointTests
{
    private readonly HttpClient _client;

    public AuthFlowEndpointTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    private static RegisterUserRequest NovoUsuario() =>
        new("Fulano Fluxo", $"fluxo-{Guid.NewGuid():N}@gatums.com", "senha123", "11999998888");

    [Fact]
    public async Task FluxoCompleto_RegisterLoginMeRefreshReusoLogout()
    {
        // 1. Registro
        var req = NovoUsuario();
        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", req);
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Login — captura accessToken e refreshToken
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = req.Email, senha = req.Senha });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var (accessToken, refreshOriginal) = await ExtrairTokens(login);
        accessToken.Should().NotBeNullOrEmpty();
        refreshOriginal.Should().NotBeNullOrEmpty();

        // 3. /me com Bearer accessToken → confirma o email
        using (var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me"))
        {
            meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var me = await _client.SendAsync(meReq);
            me.StatusCode.Should().Be(HttpStatusCode.OK);
            using var meDoc = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
            meDoc.RootElement.GetProperty("email").GetString().Should().Be(req.Email);
        }

        // 4. /refresh com o refreshToken → novo access + novo refresh (rotação)
        var refresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken = refreshOriginal });
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);

        var (novoAccess, novoRefresh) = await ExtrairTokens(refresh);
        novoAccess.Should().NotBeNullOrEmpty();
        novoRefresh.Should().NotBeNullOrEmpty();
        novoRefresh.Should().NotBe(refreshOriginal, "a rotação deve emitir um refresh token diferente");

        // 5. Reúso do refresh ORIGINAL (já rotacionado) → falha (detecção de reúso)
        var reuso = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken = refreshOriginal });
        reuso.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 6. Família revogada: o novoRefresh pertencia à mesma família revogada no passo 5.
        //    Também deve falhar — confirma a PERSISTÊNCIA da revogação de família.
        var familiaRevogada = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken = novoRefresh });
        familiaRevogada.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 7. Logout com Bearer + refreshToken → 200
        using var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout")
        {
            Content = JsonContent.Create(new { refreshToken = novoRefresh })
        };
        logoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", novoAccess);
        var logout = await _client.SendAsync(logoutReq);
        logout.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<(string AccessToken, string RefreshToken)> ExtrairTokens(HttpResponseMessage resp)
    {
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var access = root.GetProperty("accessToken").GetString() ?? string.Empty;
        var refresh = root.GetProperty("refreshToken").GetString() ?? string.Empty;
        return (access, refresh);
    }
}
