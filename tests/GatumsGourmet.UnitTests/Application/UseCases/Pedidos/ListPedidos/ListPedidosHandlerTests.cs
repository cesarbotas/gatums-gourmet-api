using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Pedidos.ListPedidos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pedidos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Pedidos.ListPedidos;

public class ListPedidosHandlerTests
{
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly ListPedidosHandler _handler;

    public ListPedidosHandlerTests() => _handler = new ListPedidosHandler(_pedidos.Object);

    private static Pedido CreatePedido(Guid usuarioId) => new()
    {
        Id = Guid.NewGuid(),
        UsuarioId = usuarioId,
        ClienteNome = "Cliente",
        Total = 50m,
        Data = DateTime.UtcNow,
        StatusAtual = PedidoEtapa.Criado,
        Itens = new List<PedidoItem>
        {
            new() { Id = Guid.NewGuid(), ProdutoId = Guid.NewGuid(), Nome = "A", Preco = 25m, FotoUrl = "u", Quantidade = 2 }
        },
        Timeline = new List<PedidoTimelineEvento>
        {
            new() { Id = Guid.NewGuid(), Etapa = PedidoEtapa.Criado, Em = DateTime.UtcNow, Por = PedidoActor.Cliente }
        }
    };

    [Fact]
    public async Task Handle_FiltroUsuario_MapeiaStatusLowercase()
    {
        var usuarioId = Guid.NewGuid();
        _pedidos.Setup(r => r.ListAsync(usuarioId, 1, 10, default))
            .ReturnsAsync((new List<Pedido> { CreatePedido(usuarioId) }, 1));

        var result = await _handler.Handle(usuarioId, new ListPedidosRequest(1, 10));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Total.Should().Be(1);
        result.Data.Items[0].StatusAtual.Should().Be("criado");
        result.Data.Items[0].Timeline[0].Por.Should().Be("cliente");
    }

    [Fact]
    public async Task Handle_FiltroNull_RetornaTodos()
    {
        _pedidos.Setup(r => r.ListAsync(null, 1, 10, default))
            .ReturnsAsync((new List<Pedido>(), 0));

        var result = await _handler.Handle(null, new ListPedidosRequest());

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        _pedidos.Verify(r => r.ListAsync(null, 1, 10, default), Times.Once);
    }
}
