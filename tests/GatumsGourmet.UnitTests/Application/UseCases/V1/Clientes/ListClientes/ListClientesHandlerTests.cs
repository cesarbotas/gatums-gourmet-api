using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Clientes.ListClientes;
using GatumsGourmet.Domain.Entities.Clientes;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Clientes.ListClientes;

public class ListClientesHandlerTests
{
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly ListClientesHandler _handler;

    public ListClientesHandlerTests() => _handler = new ListClientesHandler(_clientes.Object);

    private static Cliente CreateCliente() => new()
    {
        Id = Guid.NewGuid(),
        Nome = "Cliente",
        Email = "cliente@x.com",
        Telefone = "1199999",
        CriadoEm = DateTime.UtcNow,
        Endereco = new Endereco
        {
            Id = Guid.NewGuid(),
            ClienteId = Guid.NewGuid(),
            Logradouro = "Rua A",
            Numero = "10",
            Bairro = "Centro",
            Cidade = "SP",
            Estado = "SP",
            Cep = "01000000"
        }
    };

    [Fact]
    public async Task Handle_ComClientes_RetornaPaginadoComEndereco()
    {
        var cliente = CreateCliente();
        _clientes.Setup(r => r.ListAsync(1, 10, null, default))
            .ReturnsAsync((new List<Cliente> { cliente }, 1));

        var result = await _handler.Handle(new ListClientesRequest(1, 10, null));

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalRegistros.Should().Be(1);
        result.Data.Itens.Should().HaveCount(1);
        result.Data.Itens[0].Endereco.Cidade.Should().Be("SP");
        result.Data.Itens[0].Email.Should().Be("cliente@x.com");
    }

    [Fact]
    public async Task Handle_ComBusca_PassaFiltroParaRepositorio()
    {
        _clientes.Setup(r => r.ListAsync(2, 5, "joao", default))
            .ReturnsAsync((new List<Cliente>(), 0));

        var result = await _handler.Handle(new ListClientesRequest(2, 5, "joao"));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Itens.Should().BeEmpty();
        _clientes.Verify(r => r.ListAsync(2, 5, "joao", default), Times.Once);
    }
}

