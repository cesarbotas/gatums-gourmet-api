using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Clientes.ListClientes;

[Collection("Integration")]
public class ListClientesEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ListClientesEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static object NovoCliente() => new
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
    public async Task Listar_SemAutenticacao_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/clientes");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Listar_ComoCliente_RetornaForbidden()
    {
        var (client, _) = await _factory.CreateClienteClientAsync();

        var resp = await client.GetAsync("/api/v1/clientes");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Listar_ComoAdmin_RetornaOk()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        // Garante ao menos um cliente criado via POST antes de listar.
        await admin.PostAsJsonAsync("/api/v1/clientes", NovoCliente());

        var resp = await admin.GetAsync("/api/v1/clientes?pagina=1&tamanho=50");

        resp.IsSuccessStatusCode.Should().BeTrue();
    }
}
