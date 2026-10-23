using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Clientes.CreateCliente;
using GatumsGourmet.Domain.Entities.Clientes;
using GatumsGourmet.Domain.Services.Interfaces;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Clientes.CreateCliente;

public class CreateClienteHandlerTests
{
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<IClock> _clock = new();
    private readonly CreateClienteHandler _handler;
    private readonly DateTime _agora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public CreateClienteHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(_agora);
        _handler = new CreateClienteHandler(_clientes.Object, _clock.Object);
    }

    private static CreateClienteRequest CreateRequest() => new(
        "Cliente Novo", "novo@x.com", "1198888",
        new EnderecoRequest("Rua B", "20", "Ap 1", "Centro", "SP", "SP", "01010000"));

    [Fact]
    public async Task Handle_Valido_CriaClienteERetornaId()
    {
        Cliente? adicionado = null;
        _clientes.Setup(r => r.AddAsync(It.IsAny<Cliente>(), default))
            .Callback<Cliente, CancellationToken>((c, _) => adicionado = c)
            .Returns(Task.CompletedTask);
        _clientes.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(CreateRequest());

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().NotBeEmpty();
        adicionado.Should().NotBeNull();
        adicionado!.Nome.Should().Be("Cliente Novo");
        adicionado.CriadoEm.Should().Be(_agora);
        adicionado.Endereco.Logradouro.Should().Be("Rua B");
        _clientes.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}

