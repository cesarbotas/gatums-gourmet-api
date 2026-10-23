using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Auth.RegisterUser;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Auth.Login;

[Collection("Integration")]
public class LoginEndpointTests
{
    private readonly HttpClient _client;

    public LoginEndpointTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    private static RegisterUserRequest NovoUsuario() =>
        new("Fulano", $"login-{Guid.NewGuid():N}@gatums.com", "senha123", "11999998888");

    [Fact]
    public async Task Login_AposRegistro_RetornaTokenERoleCliente()
    {
        var req = NovoUsuario();
        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", req);
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = req.Email, senha = req.Senha });

        login.IsSuccessStatusCode.Should().BeTrue();
        using var doc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        doc.RootElement.GetProperty("user").GetProperty("role").GetString().Should().Be("cliente");
    }

    [Fact]
    public async Task Login_CredenciaisInvalidas_RetornaBadRequest()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "naoexiste@gatums.com", senha = "errada" });

        login.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
