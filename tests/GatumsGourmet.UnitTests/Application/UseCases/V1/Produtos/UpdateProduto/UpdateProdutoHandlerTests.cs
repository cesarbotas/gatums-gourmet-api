using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Produtos.UpdateProduto;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Enums.Produtos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Produtos.UpdateProduto;

public class UpdateProdutoHandlerTests
{
    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly Mock<ICategoriaRepository> _categorias = new();
    private readonly UpdateProdutoHandler _handler;

    public UpdateProdutoHandlerTests()
    {
        _categorias.Setup(c => c.GetByIdAsync(It.IsAny<long>(), default))
            .ReturnsAsync(new Categoria { Id = 2, Nome = "Cat2", Ativo = true });
        _handler = new UpdateProdutoHandler(_produtos.Object, _categorias.Object);
    }

    [Fact]
    public async Task Handle_Existente_AtualizaCamposERetornaTrue()
    {
        var produto = new Produto { Id = Guid.NewGuid(), Nome = "Antigo", Descricao = "d", CategoriaId = 1, Preco = 10, FotoUrl = "u", Disponivel = true, Tipo = TipoProduto.Simples };
        _produtos.Setup(r => r.GetByIdAsync(produto.Id, default)).ReturnsAsync(produto);
        _produtos.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new UpdateProdutoRequest(produto.Id, "Novo", "desc2", 2, 99.90m, "url2", false));

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        produto.Nome.Should().Be("Novo");
        produto.CategoriaId.Should().Be(2);
        produto.Preco.Should().Be(99.90m);
        produto.Disponivel.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Inexistente_RetornaErro()
    {
        _produtos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Produto?)null);

        var result = await _handler.Handle(new UpdateProdutoRequest(Guid.NewGuid(), "X", "d", 1, 1, "u", true));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Produto não encontrado.");
    }

    [Fact]
    public async Task Handle_CategoriaInexistente_RetornaErro()
    {
        var produto = new Produto { Id = Guid.NewGuid(), Nome = "P", Descricao = "d", CategoriaId = 1, Preco = 10, FotoUrl = "u", Disponivel = true, Tipo = TipoProduto.Simples };
        _produtos.Setup(r => r.GetByIdAsync(produto.Id, default)).ReturnsAsync(produto);
        _categorias.Setup(c => c.GetByIdAsync(It.IsAny<long>(), default)).ReturnsAsync((Categoria?)null);

        var result = await _handler.Handle(new UpdateProdutoRequest(produto.Id, "X", "d", 999, 1, "u", true));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Categoria não encontrada.");
    }
}

