using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.Categorias.ListCategorias;
using GatumsGourmet.Domain.Entities.Produtos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Categorias.ListCategorias;

public class ListCategoriasHandlerTests
{
    private readonly Mock<ICategoriaRepository> _categorias = new();
    private readonly ListCategoriasHandler _handler;

    public ListCategoriasHandlerTests() => _handler = new ListCategoriasHandler(_categorias.Object);

    [Fact]
    public async Task Handle_RetornaCategoriasMapeadas()
    {
        var lista = new List<Categoria>
        {
            new() { Id = 1, Nome = "Bebidas", Descricao = "d", Ativo = true },
            new() { Id = 2, Nome = "Lanches", Descricao = null, Ativo = false }
        };
        _categorias.Setup(r => r.ListAsync(null, default)).ReturnsAsync(lista);

        var result = await _handler.Handle(null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(2);
        result.Data[0].Nome.Should().Be("Bebidas");
        result.Data[1].Ativo.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ApenasAtivas_PassaFiltro()
    {
        _categorias.Setup(r => r.ListAsync(true, default)).ReturnsAsync(new List<Categoria>());

        var result = await _handler.Handle(true);

        result.IsSuccess.Should().BeTrue();
        _categorias.Verify(r => r.ListAsync(true, default), Times.Once);
    }
}

