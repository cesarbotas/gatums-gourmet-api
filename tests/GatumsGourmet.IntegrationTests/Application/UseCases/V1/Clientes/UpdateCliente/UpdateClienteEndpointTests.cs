using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Clientes.UpdateCliente;

[Collection("Integration")]
public class UpdateClienteEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public UpdateClienteEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

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

    private static object UpdateBody() => new
    {
        nome = "Cliente Atualizado",
        email = $"upd-{Guid.NewGuid():N}@x.com",
        telefone = "11911112222"
    };

    [Fact]
    public async Task Update_ComoAdmin_ClienteExistente_RetornaOk()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        // Cria o cliente via POST e captura o Id para atualizar em seguida.
        var create = await admin.PostAsJsonAsync("/api/v1/clientes", NovoCliente());
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var resp = await admin.PutAsJsonAsync($"/api/v1/clientes/{id}", UpdateBody());

        resp.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ClienteInexistente_RetornaBadRequest()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var resp = await admin.PutAsJsonAsync($"/api/v1/clientes/{Guid.NewGuid()}", UpdateBody());

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
