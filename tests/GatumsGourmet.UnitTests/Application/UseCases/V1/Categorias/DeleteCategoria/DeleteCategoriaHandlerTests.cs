using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Categorias.DeleteCategoria;
using GatumsGourmet.Domain.Entities.Produtos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Categorias.DeleteCategoria;

public class DeleteCategoriaHandlerTests
{
    private readonly Mock<ICategoriaRepository> _categorias = new();
    private readonly DeleteCategoriaHandler _handler;

    public DeleteCategoriaHandlerTests() => _handler = new DeleteCategoriaHandler(_categorias.Object);

    [Fact]
    public async Task Handle_SemProdutos_RemoveERetornaTrue()
    {
        var cat = new Categoria { Id = 5, Nome = "Vazia", Ativo = true };
        _categorias.Setup(r => r.GetByIdAsync(5, default)).ReturnsAsync(cat);
        _categorias.Setup(r => r.TemProdutosVinculadosAsync(5, default)).ReturnsAsync(false);
        _categorias.Setup(r => r.DeleteAsync(cat, default)).Returns(Task.CompletedTask);
        _categorias.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(5);

        result.IsSuccess.Should().BeTrue();
        _categorias.Verify(r => r.DeleteAsync(cat, default), Times.Once);
    }

    [Fact]
    public async Task Handle_Inexistente_RetornaErro()
    {
        _categorias.Setup(r => r.GetByIdAsync(It.IsAny<long>(), default)).ReturnsAsync((Categoria?)null);

        var result = await _handler.Handle(99);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Categoria não encontrada.");
    }

    [Fact]
    public async Task Handle_ComProdutosVinculados_RetornaErro()
    {
        var cat = new Categoria { Id = 5, Nome = "EmUso", Ativo = true };
        _categorias.Setup(r => r.GetByIdAsync(5, default)).ReturnsAsync(cat);
        _categorias.Setup(r => r.TemProdutosVinculadosAsync(5, default)).ReturnsAsync(true);

        var result = await _handler.Handle(5);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Não é possível excluir: categoria possui produtos vinculados.");
        _categorias.Verify(r => r.DeleteAsync(It.IsAny<Categoria>(), default), Times.Never);
    }
}

