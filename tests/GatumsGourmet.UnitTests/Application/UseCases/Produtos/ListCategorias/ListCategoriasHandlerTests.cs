using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Produtos.ListCategorias;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Produtos.ListCategorias;

public class ListCategoriasHandlerTests
{
    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly ListCategoriasHandler _handler;

    public ListCategoriasHandlerTests() => _handler = new ListCategoriasHandler(_produtos.Object);

    [Fact]
    public async Task Handle_RetornaCategoriasDoRepositorio()
    {
        var categorias = new List<string> { "Hambúrgueres", "Bebidas", "Pizzas" };
        _produtos.Setup(r => r.ListCategoriasAsync(default)).ReturnsAsync(categorias);

        var result = await _handler.Handle();

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(categorias);
    }

    [Fact]
    public async Task Handle_SemCategorias_RetornaListaVazia()
    {
        _produtos.Setup(r => r.ListCategoriasAsync(default)).ReturnsAsync(new List<string>());

        var result = await _handler.Handle();

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
