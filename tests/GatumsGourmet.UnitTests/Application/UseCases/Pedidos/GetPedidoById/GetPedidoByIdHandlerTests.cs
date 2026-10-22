using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.Pedidos.GetPedidoById;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pedidos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Pedidos.GetPedidoById;

public class GetPedidoByIdHandlerTests
{
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly GetPedidoByIdHandler _handler;

    public GetPedidoByIdHandlerTests() => _handler = new GetPedidoByIdHandler(_pedidos.Object);

    private static Pedido CreatePedido(Guid usuarioId) => new()
    {
        Id = Guid.NewGuid(), UsuarioId = usuarioId, ClienteNome = "C", Total = 10m,
        Data = DateTime.UtcNow, StatusAtual = PedidoEtapa.Criado
    };

    [Fact]
    public async Task Handle_Inexistente_RetornaErro()
    {
        _pedidos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Pedido?)null);

        var result = await _handler.Handle(Guid.NewGuid(), Guid.NewGuid(), isAdmin: false);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Pedido não encontrado.");
    }

    [Fact]
    public async Task Handle_ClienteDonoDoPedido_RetornaPedido()
    {
        var usuarioId = Guid.NewGuid();
        var pedido = CreatePedido(usuarioId);
        _pedidos.Setup(r => r.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);

        var result = await _handler.Handle(pedido.Id, usuarioId, isAdmin: false);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(pedido.Id);
    }

    [Fact]
    public async Task Handle_ClienteNaoDono_RetornaAcessoNegado()
    {
        var pedido = CreatePedido(Guid.NewGuid());
        _pedidos.Setup(r => r.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);

        var result = await _handler.Handle(pedido.Id, Guid.NewGuid(), isAdmin: false);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Acesso negado.");
    }

    [Fact]
    public async Task Handle_AdminPedidoDeOutro_RetornaPedido()
    {
        var pedido = CreatePedido(Guid.NewGuid());
        _pedidos.Setup(r => r.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);

        var result = await _handler.Handle(pedido.Id, Guid.NewGuid(), isAdmin: true);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(pedido.Id);
    }
}

