using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Categorias.GetCategoriaById;
using GatumsGourmet.Domain.Entities.Produtos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Categorias.GetCategoriaById;

public class GetCategoriaByIdHandlerTests
{
    private readonly Mock<ICategoriaRepository> _categorias = new();
    private readonly GetCategoriaByIdHandler _handler;

    public GetCategoriaByIdHandlerTests() => _handler = new GetCategoriaByIdHandler(_categorias.Object);

    [Fact]
    public async Task Handle_Existente_RetornaCategoria()
    {
        _categorias.Setup(r => r.GetByIdAsync(10, default))
            .ReturnsAsync(new Categoria { Id = 10, Nome = "Bebidas", Descricao = "d", Ativo = true });

        var result = await _handler.Handle(10);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(10);
        result.Data.Nome.Should().Be("Bebidas");
    }

    [Fact]
    public async Task Handle_Inexistente_RetornaErro()
    {
        _categorias.Setup(r => r.GetByIdAsync(It.IsAny<long>(), default)).ReturnsAsync((Categoria?)null);

        var result = await _handler.Handle(999);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Categoria não encontrada.");
    }
}

