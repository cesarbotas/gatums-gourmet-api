using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.Carrinho.ClearCarrinho;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Carrinho.ClearCarrinho;

public class ClearCarrinhoHandlerTests
{
    private readonly Mock<ICarrinhoRepository> _carrinho = new();
    private readonly ClearCarrinhoHandler _handler;

    public ClearCarrinhoHandlerTests() => _handler = new ClearCarrinhoHandler(_carrinho.Object);

    [Fact]
    public async Task Handle_LimpaCarrinhoERetornaTrue()
    {
        var usuarioId = Guid.NewGuid();
        _carrinho.Setup(r => r.ClearAsync(usuarioId, default)).Returns(Task.CompletedTask);
        _carrinho.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(usuarioId);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        _carrinho.Verify(r => r.ClearAsync(usuarioId, default), Times.Once);
        _carrinho.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}

