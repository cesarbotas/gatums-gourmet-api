using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Produtos.DeleteProduto;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Enums.Produtos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Produtos.DeleteProduto;

public class DeleteProdutoHandlerTests
{
    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly DeleteProdutoHandler _handler;

    public DeleteProdutoHandlerTests() => _handler = new DeleteProdutoHandler(_produtos.Object);

    [Fact]
    public async Task Handle_Existente_RemoveERetornaTrue()
    {
        var produto = new Produto { Id = Guid.NewGuid(), Nome = "X", Tipo = TipoProduto.Simples };
        _produtos.Setup(r => r.GetByIdAsync(produto.Id, default)).ReturnsAsync(produto);
        _produtos.Setup(r => r.DeleteAsync(produto, default)).Returns(Task.CompletedTask);
        _produtos.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(produto.Id);

        result.IsSuccess.Should().BeTrue();
        _produtos.Verify(r => r.DeleteAsync(produto, default), Times.Once);
        _produtos.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_Inexistente_RetornaErro()
    {
        _produtos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Produto?)null);

        var result = await _handler.Handle(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Produto não encontrado.");
    }
}

