using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Clientes.DeleteCliente;

[Collection("Integration")]
public class DeleteClienteEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public DeleteClienteEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

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
    public async Task Delete_ComoAdmin_RemoveCliente()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        // Cria o cliente via POST e captura o Id para removê-lo em seguida.
        var create = await admin.PostAsJsonAsync("/api/v1/clientes", NovoCliente());
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var resp = await admin.DeleteAsync($"/api/v1/clientes/{id}");

        resp.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_Inexistente_RetornaBadRequest()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var resp = await admin.DeleteAsync($"/api/v1/clientes/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
