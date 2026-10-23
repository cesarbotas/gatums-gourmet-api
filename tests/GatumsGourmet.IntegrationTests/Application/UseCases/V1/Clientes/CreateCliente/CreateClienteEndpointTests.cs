using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Clientes.CreateCliente;

[Collection("Integration")]
public class CreateClienteEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public CreateClienteEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    public static object NovoCliente() => new
    {
        nome = $"Cliente {Guid.NewGuid():N}",
        email = $"cli-{Guid.NewGuid():N}@x.com",
        telefone = "11988887777",
        endereco = new
        {
            logradouro = "Rua das Flores",
            numero = "123",
            complemento = "Ap 4",
            bairro = "Centro",
            cidade = "São Paulo",
            estado = "SP",
            cep = "01000000"
        }
    };

    [Fact]
    public async Task Criar_SemAutenticacao_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/clientes", NovoCliente());

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Criar_ComoCliente_RetornaForbidden()
    {
        var (client, _) = await _factory.CreateClienteClientAsync();

        var resp = await client.PostAsJsonAsync("/api/v1/clientes", NovoCliente());

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Criar_ComoAdmin_RetornaCreatedComId()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var resp = await admin.PostAsJsonAsync("/api/v1/clientes", NovoCliente());

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }
}
