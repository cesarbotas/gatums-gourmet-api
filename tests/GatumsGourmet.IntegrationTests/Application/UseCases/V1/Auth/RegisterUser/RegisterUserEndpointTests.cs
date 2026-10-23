using System.Net.Http.Json;
using System.Text.Json;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;
using Xunit.Abstractions;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Auth.RegisterUser;

/// <summary>
/// Teste base de endpoint exercitando o fluxo completo de registro via HTTP contra a API real.
/// </summary>
[Collection("Integration")]
public class RegisterUserEndpointTests
{
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;

    public RegisterUserEndpointTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _httpClient = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Register_FluxoCompleto_DeveRetornarSucesso()
    {
        var request = RegisterUserRequestFixture.Sucesso();

        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/register", request, CancellationToken.None);

        var body = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Status: {(int)response.StatusCode} - Body: {body}");

        Assert.True(response.IsSuccessStatusCode, $"Status inesperado: {(int)response.StatusCode}");

        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("id", out var id), "Resposta não contém a propriedade 'id'.");
        Assert.NotEqual(Guid.Empty, id.GetGuid());
    }
}
