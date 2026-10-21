using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Produtos.UpdateProduto;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Enums.Produtos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Produtos.UpdateProduto;

public class UpdateProdutoHandlerTests
{
    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly UpdateProdutoHandler _handler;

    public UpdateProdutoHandlerTests() => _handler = new UpdateProdutoHandler(_produtos.Object);

    [Fact]
    public async Task Handle_Existente_AtualizaCamposERetornaTrue()
    {
        var produto = new Produto { Id = Guid.NewGuid(), Nome = "Antigo", Descricao = "d", Categoria = "C", Preco = 10, FotoUrl = "u", Disponivel = true, Tipo = TipoProduto.Simples };
        _produtos.Setup(r => r.GetByIdAsync(produto.Id, default)).ReturnsAsync(produto);
        _produtos.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new UpdateProdutoRequest(produto.Id, "Novo", "desc2", "Cat2", 99.90m, "url2", false));

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        produto.Nome.Should().Be("Novo");
        produto.Preco.Should().Be(99.90m);
        produto.Disponivel.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Inexistente_RetornaErro()
    {
        _produtos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Produto?)null);

        var result = await _handler.Handle(new UpdateProdutoRequest(Guid.NewGuid(), "X", "d", "C", 1, "u", true));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Produto não encontrado.");
    }
}
