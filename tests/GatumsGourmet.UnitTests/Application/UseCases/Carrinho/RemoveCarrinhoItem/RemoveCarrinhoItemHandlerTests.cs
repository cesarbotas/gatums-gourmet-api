using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.Carrinho.RemoveCarrinhoItem;
using GatumsGourmet.Domain.Entities.Carrinho;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Carrinho.RemoveCarrinhoItem;

public class RemoveCarrinhoItemHandlerTests
{
    private readonly Mock<ICarrinhoRepository> _carrinho = new();
    private readonly RemoveCarrinhoItemHandler _handler;

    public RemoveCarrinhoItemHandlerTests() => _handler = new RemoveCarrinhoItemHandler(_carrinho.Object);

    [Fact]
    public async Task Handle_ItemExistente_RemoveERetornaTrue()
    {
        var usuarioId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        var item = new CarrinhoItem { Id = Guid.NewGuid(), UsuarioId = usuarioId, ProdutoId = produtoId, Nome = "X", Preco = 10m, FotoUrl = "u", Quantidade = 1 };
        _carrinho.Setup(r => r.GetItemAsync(usuarioId, produtoId, default)).ReturnsAsync(item);
        _carrinho.Setup(r => r.RemoveAsync(item, default)).Returns(Task.CompletedTask);
        _carrinho.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(usuarioId, produtoId);

        result.IsSuccess.Should().BeTrue();
        _carrinho.Verify(r => r.RemoveAsync(item, default), Times.Once);
        _carrinho.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ItemInexistente_RetornaErro()
    {
        _carrinho.Setup(r => r.GetItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default)).ReturnsAsync((CarrinhoItem?)null);

        var result = await _handler.Handle(Guid.NewGuid(), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Item não encontrado no carrinho.");
        _carrinho.Verify(r => r.RemoveAsync(It.IsAny<CarrinhoItem>(), default), Times.Never);
    }
}

