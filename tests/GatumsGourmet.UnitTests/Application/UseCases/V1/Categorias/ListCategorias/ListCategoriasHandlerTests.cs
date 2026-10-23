using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Categorias.ListCategorias;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Repositories;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Categorias.ListCategorias;

public class ListCategoriasHandlerTests
{
    private readonly Mock<ICategoriaRepository> _categorias = new();
    private readonly ListCategoriasHandler _handler;

    public ListCategoriasHandlerTests() => _handler = new ListCategoriasHandler(_categorias.Object);

    [Fact]
    public async Task Handle_RetornaCategoriasPaginadas()
    {
        var lista = new List<Categoria>
        {
            new() { Id = 1, Nome = "Bebidas", Descricao = "d", Ativo = true },
            new() { Id = 2, Nome = "Lanches", Descricao = null, Ativo = false }
        };
        _categorias.Setup(r => r.ListAsync(1, 10, null, default)).ReturnsAsync((lista, 2));

        var result = await _handler.Handle(new ListCategoriasRequest(1, 10, null));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Itens.Should().HaveCount(2);
        result.Data.TotalRegistros.Should().Be(2);
        result.Data.PaginaAtual.Should().Be(1);
        result.Data.TotalPaginas.Should().Be(1);
        result.Data.Itens[0].Nome.Should().Be("Bebidas");
    }

    [Fact]
    public async Task Handle_ApenasAtivas_PassaFiltro()
    {
        _categorias.Setup(r => r.ListAsync(1, 10, true, default)).ReturnsAsync((new List<Categoria>(), 0));

        var result = await _handler.Handle(new ListCategoriasRequest(1, 10, true));

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalRegistros.Should().Be(0);
        result.Data.TotalPaginas.Should().Be(0);
        _categorias.Verify(r => r.ListAsync(1, 10, true, default), Times.Once);
    }
}
