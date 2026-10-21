using System.Net;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Auth.GetCurrentUser;

[Collection("Integration")]
public class GetCurrentUserEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public GetCurrentUserEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Me_SemToken_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/auth/me");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ComToken_RetornaUsuarioAtual()
    {
        var (client, user) = await _factory.CreateClienteClientAsync();

        var resp = await client.GetAsync("/api/v1/auth/me");

        resp.IsSuccessStatusCode.Should().BeTrue();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("email").GetString().Should().Be(user.Email);
    }
}
