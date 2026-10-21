using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Carrinho.UpdateCarrinhoItemQuantidade;
using GatumsGourmet.Domain.Entities.Carrinho;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Carrinho.UpdateCarrinhoItemQuantidade;

public class UpdateCarrinhoItemQuantidadeHandlerTests
{
    private readonly Mock<ICarrinhoRepository> _carrinho = new();
    private readonly UpdateCarrinhoItemQuantidadeHandler _handler;

    public UpdateCarrinhoItemQuantidadeHandlerTests() => _handler = new UpdateCarrinhoItemQuantidadeHandler(_carrinho.Object);

    private static CarrinhoItem CreateItem(Guid usuarioId, Guid produtoId) => new()
    {
        Id = Guid.NewGuid(), UsuarioId = usuarioId, ProdutoId = produtoId,
        Nome = "X", Preco = 10m, FotoUrl = "u", Quantidade = 1
    };

    [Fact]
    public async Task Handle_ItemInexistente_RetornaErro()
    {
        _carrinho.Setup(r => r.GetItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default)).ReturnsAsync((CarrinhoItem?)null);

        var result = await _handler.Handle(Guid.NewGuid(), Guid.NewGuid(), 5);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Item não encontrado no carrinho.");
    }

    [Fact]
    public async Task Handle_QuantidadePositiva_AtualizaQuantidade()
    {
        var usuarioId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        var item = CreateItem(usuarioId, produtoId);
        _carrinho.Setup(r => r.GetItemAsync(usuarioId, produtoId, default)).ReturnsAsync(item);
        _carrinho.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(usuarioId, produtoId, 7);

        result.IsSuccess.Should().BeTrue();
        item.Quantidade.Should().Be(7);
        _carrinho.Verify(r => r.RemoveAsync(It.IsAny<CarrinhoItem>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_QuantidadeZeroOuNegativa_RemoveItem()
    {
        var usuarioId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        var item = CreateItem(usuarioId, produtoId);
        _carrinho.Setup(r => r.GetItemAsync(usuarioId, produtoId, default)).ReturnsAsync(item);
        _carrinho.Setup(r => r.RemoveAsync(item, default)).Returns(Task.CompletedTask);
        _carrinho.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(usuarioId, produtoId, 0);

        result.IsSuccess.Should().BeTrue();
        _carrinho.Verify(r => r.RemoveAsync(item, default), Times.Once);
    }
}
