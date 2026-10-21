using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Clientes.DeleteCliente;
using GatumsGourmet.Domain.Entities.Clientes;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Clientes.DeleteCliente;

public class DeleteClienteHandlerTests
{
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly DeleteClienteHandler _handler;

    public DeleteClienteHandlerTests() => _handler = new DeleteClienteHandler(_clientes.Object);

    [Fact]
    public async Task Handle_Existente_RemoveERetornaTrue()
    {
        var cliente = new Cliente { Id = Guid.NewGuid(), Nome = "X", Email = "x@x.com" };
        _clientes.Setup(r => r.GetByIdAsync(cliente.Id, default)).ReturnsAsync(cliente);
        _clientes.Setup(r => r.DeleteAsync(cliente, default)).Returns(Task.CompletedTask);
        _clientes.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(cliente.Id);

        result.IsSuccess.Should().BeTrue();
        _clientes.Verify(r => r.DeleteAsync(cliente, default), Times.Once);
        _clientes.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_Inexistente_RetornaErro()
    {
        _clientes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Cliente?)null);

        var result = await _handler.Handle(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cliente não encontrado.");
        _clientes.Verify(r => r.DeleteAsync(It.IsAny<Cliente>(), default), Times.Never);
    }
}
