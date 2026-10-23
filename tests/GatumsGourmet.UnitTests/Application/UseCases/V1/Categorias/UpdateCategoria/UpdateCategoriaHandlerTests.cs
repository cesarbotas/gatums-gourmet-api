using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Categorias.UpdateCategoria;
using GatumsGourmet.Domain.Entities.Produtos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Categorias.UpdateCategoria;

public class UpdateCategoriaHandlerTests
{
    private readonly Mock<ICategoriaRepository> _categorias = new();
    private readonly UpdateCategoriaHandler _handler;

    public UpdateCategoriaHandlerTests() => _handler = new UpdateCategoriaHandler(_categorias.Object);

    [Fact]
    public async Task Handle_Existente_AtualizaCampos()
    {
        var cat = new Categoria { Id = 3, Nome = "Antigo", Descricao = "x", Ativo = true };
        _categorias.Setup(r => r.GetByIdAsync(3, default)).ReturnsAsync(cat);
        _categorias.Setup(r => r.ExistsByNomeAsync("Novo", 3, default)).ReturnsAsync(false);
        _categorias.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new UpdateCategoriaRequest(3, "Novo", "nova desc", false));

        result.IsSuccess.Should().BeTrue();
        cat.Nome.Should().Be("Novo");
        cat.Descricao.Should().Be("nova desc");
        cat.Ativo.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Inexistente_RetornaErro()
    {
        _categorias.Setup(r => r.GetByIdAsync(It.IsAny<long>(), default)).ReturnsAsync((Categoria?)null);

        var result = await _handler.Handle(new UpdateCategoriaRequest(99, "X", null, true));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Categoria não encontrada.");
    }

    [Fact]
    public async Task Handle_NomeDuplicadoEmOutra_RetornaErro()
    {
        var cat = new Categoria { Id = 3, Nome = "Antigo", Ativo = true };
        _categorias.Setup(r => r.GetByIdAsync(3, default)).ReturnsAsync(cat);
        _categorias.Setup(r => r.ExistsByNomeAsync("Bebidas", 3, default)).ReturnsAsync(true);

        var result = await _handler.Handle(new UpdateCategoriaRequest(3, "Bebidas", null, true));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Já existe uma categoria com este nome.");
    }
}

