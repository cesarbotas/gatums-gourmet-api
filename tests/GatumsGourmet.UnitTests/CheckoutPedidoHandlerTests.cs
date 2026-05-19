using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Pedidos.CheckoutPedido;
using GatumsGourmet.Domain.Entities.Carrinho;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Entities.Vendas;
using GatumsGourmet.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GatumsGourmet.UnitTests;

public class CheckoutPedidoHandlerTests
{
    private readonly Mock<ICarrinhoRepository> _carrinho = new();
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly Mock<IVendaRepository> _vendas = new();
    private readonly Mock<IClock> _clock = new();
    private readonly CheckoutPedidoHandler _handler;

    public CheckoutPedidoHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        _handler = new CheckoutPedidoHandler(_carrinho.Object, _pedidos.Object, _vendas.Object, _clock.Object, NullLogger<CheckoutPedidoHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CarrinhoVazio_RetornaErro()
    {
        var userId = Guid.NewGuid();
        _carrinho.Setup(c => c.GetByUsuarioAsync(userId, default)).ReturnsAsync([]);

        var result = await _handler.Handle(userId, "João", default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("vazio");
    }

    [Fact]
    public async Task Handle_CarrinhoComItens_CriaPedidoELimpaCarrinho()
    {
        var userId = Guid.NewGuid();
        var itens = new List<CarrinhoItem>
        {
            new() { Id = Guid.NewGuid(), UsuarioId = userId, ProdutoId = Guid.NewGuid(), Nome = "Hambúrguer", Preco = 32.90m, FotoUrl = "url", Quantidade = 2 }
        };

        _carrinho.Setup(c => c.GetByUsuarioAsync(userId, default)).ReturnsAsync(itens);
        _pedidos.Setup(p => p.AddAsync(It.IsAny<Pedido>(), default)).Returns(Task.CompletedTask);
        _pedidos.Setup(p => p.SaveChangesAsync(default)).Returns(Task.CompletedTask);
        _vendas.Setup(v => v.AddRangeAsync(It.IsAny<IEnumerable<Venda>>(), default)).Returns(Task.CompletedTask);
        _carrinho.Setup(c => c.ClearAsync(userId, default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(userId, "João", default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.PedidoId.Should().NotBeEmpty();
        _pedidos.Verify(p => p.AddAsync(It.Is<Pedido>(p => p.Total == 65.80m), default), Times.Once);
        _carrinho.Verify(c => c.ClearAsync(userId, default), Times.Once);
    }
}
