using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.Categorias.CreateCategoria;
using GatumsGourmet.Domain.Entities.Produtos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Categorias.CreateCategoria;

public class CreateCategoriaHandlerTests
{
    private readonly Mock<ICategoriaRepository> _categorias = new();
    private readonly CreateCategoriaHandler _handler;

    public CreateCategoriaHandlerTests() => _handler = new CreateCategoriaHandler(_categorias.Object);

    [Fact]
    public async Task Handle_NomeUnico_CriaERetornaId()
    {
        _categorias.Setup(r => r.ExistsByNomeAsync("Sobremesas", null, default)).ReturnsAsync(false);
        Categoria? criada = null;
        _categorias.Setup(r => r.AddAsync(It.IsAny<Categoria>(), default))
            .Callback<Categoria, CancellationToken>((c, _) => { c.Id = 7; criada = c; })
            .Returns(Task.CompletedTask);
        _categorias.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new CreateCategoriaRequest("Sobremesas", "Doces", true));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(7);
        criada!.Nome.Should().Be("Sobremesas");
        criada.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NomeDuplicado_RetornaErro()
    {
        _categorias.Setup(r => r.ExistsByNomeAsync("Bebidas", null, default)).ReturnsAsync(true);

        var result = await _handler.Handle(new CreateCategoriaRequest("Bebidas", null, true));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Já existe uma categoria com este nome.");
        _categorias.Verify(r => r.AddAsync(It.IsAny<Categoria>(), default), Times.Never);
    }
}

