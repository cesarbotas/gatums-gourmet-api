using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Carrinho.GetCarrinho;
using GatumsGourmet.Domain.Entities.Carrinho;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Carrinho.GetCarrinho;

public class GetCarrinhoHandlerTests
{
    private readonly Mock<ICarrinhoRepository> _carrinho = new();
    private readonly GetCarrinhoHandler _handler;

    public GetCarrinhoHandlerTests() => _handler = new GetCarrinhoHandler(_carrinho.Object);

    [Fact]
    public async Task Handle_ComItens_CalculaTotal()
    {
        var usuarioId = Guid.NewGuid();
        var itens = new List<CarrinhoItem>
        {
            new() { Id = Guid.NewGuid(), UsuarioId = usuarioId, ProdutoId = Guid.NewGuid(), Nome = "A", Preco = 10m, FotoUrl = "u", Quantidade = 2 },
            new() { Id = Guid.NewGuid(), UsuarioId = usuarioId, ProdutoId = Guid.NewGuid(), Nome = "B", Preco = 5.5m, FotoUrl = "u", Quantidade = 1 }
        };
        _carrinho.Setup(r => r.GetByUsuarioAsync(usuarioId, default)).ReturnsAsync(itens);

        var result = await _handler.Handle(usuarioId);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Itens.Should().HaveCount(2);
        result.Data.Total.Should().Be(25.5m);
    }

    [Fact]
    public async Task Handle_CarrinhoVazio_TotalZero()
    {
        var usuarioId = Guid.NewGuid();
        _carrinho.Setup(r => r.GetByUsuarioAsync(usuarioId, default)).ReturnsAsync(new List<CarrinhoItem>());

        var result = await _handler.Handle(usuarioId);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Itens.Should().BeEmpty();
        result.Data.Total.Should().Be(0m);
    }
}
