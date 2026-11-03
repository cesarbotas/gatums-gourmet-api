using System.Net;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Pagamentos.ListOpcoesPagamento;

[Collection("Integration")]
public class ListOpcoesPagamentoEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ListOpcoesPagamentoEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Opcoes_ComoCliente_RetornaTres()
    {
        var (cliente, _) = await _factory.CreateClienteClientAsync();

        var resp = await cliente.GetAsync("/api/v1/pagamentos/opcoes");

        resp.IsSuccessStatusCode.Should().BeTrue();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetArrayLength().Should().Be(3);
    }

    // Após a Task 11 a rota deixou de ser "ClienteOnly" e passou a exigir apenas
    // autenticação (RequireAuthorization). Portanto um admin autenticado agora
    // recebe 200, não mais 403 por não ter a role Cliente.
    [Fact]
    public async Task Opcoes_ComoAdmin_RetornaOk()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var resp = await admin.GetAsync("/api/v1/pagamentos/opcoes");

        resp.IsSuccessStatusCode.Should().BeTrue();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task Opcoes_SemAutenticacao_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/pagamentos/opcoes");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
