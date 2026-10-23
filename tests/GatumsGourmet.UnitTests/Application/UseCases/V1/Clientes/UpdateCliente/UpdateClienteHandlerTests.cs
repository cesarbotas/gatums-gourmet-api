using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Clientes.UpdateCliente;
using GatumsGourmet.Domain.Entities.Clientes;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Clientes.UpdateCliente;

public class UpdateClienteHandlerTests
{
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly UpdateClienteHandler _handler;

    public UpdateClienteHandlerTests() => _handler = new UpdateClienteHandler(_clientes.Object);

    [Fact]
    public async Task Handle_Existente_AtualizaCampos()
    {
        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            Nome = "Antigo",
            Email = "antigo@x.com",
            Telefone = "111"
        };
        _clientes.Setup(r => r.GetByIdAsync(cliente.Id, default)).ReturnsAsync(cliente);
        _clientes.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new UpdateClienteRequest(cliente.Id, "Novo", "novo@x.com", "222"));

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        cliente.Nome.Should().Be("Novo");
        cliente.Email.Should().Be("novo@x.com");
        cliente.Telefone.Should().Be("222");
        _clientes.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_Inexistente_RetornaErro()
    {
        _clientes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Cliente?)null);

        var result = await _handler.Handle(new UpdateClienteRequest(Guid.NewGuid(), "x", "x@x.com", null));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cliente não encontrado.");
        _clientes.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }
}

