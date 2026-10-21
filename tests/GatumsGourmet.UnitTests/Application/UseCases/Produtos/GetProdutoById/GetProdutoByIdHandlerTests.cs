using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Produtos.GetProdutoById;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Enums.Produtos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Produtos.GetProdutoById;

public class GetProdutoByIdHandlerTests
{
    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly GetProdutoByIdHandler _handler;

    public GetProdutoByIdHandlerTests() => _handler = new GetProdutoByIdHandler(_produtos.Object);

    [Fact]
    public async Task Handle_Existente_RetornaDetalheComItens()
    {
        var produto = new Produto
        {
            Id = Guid.NewGuid(), Nome = "Combo", Descricao = "d", Categoria = "C",
            Preco = 89.90m, FotoUrl = "url", Disponivel = true, Tipo = TipoProduto.Combo,
            ComboItens = new List<ProdutoComboItem>
            {
                new() { Id = Guid.NewGuid(), ProdutoId = Guid.NewGuid(), Quantidade = 2 }
            }
        };
        _produtos.Setup(r => r.GetByIdAsync(produto.Id, default)).ReturnsAsync(produto);

        var result = await _handler.Handle(produto.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Nome.Should().Be("Combo");
        result.Data.Tipo.Should().Be("combo");
        result.Data.Itens.Should().HaveCount(1);
        result.Data.Itens[0].Quantidade.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Inexistente_RetornaErro()
    {
        _produtos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Produto?)null);

        var result = await _handler.Handle(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Produto não encontrado.");
    }

    [Fact]
    public async Task Handle_ProdutoSimples_SemItens()
    {
        var produto = new Produto
        {
            Id = Guid.NewGuid(), Nome = "X", Descricao = "d", Categoria = "C",
            Preco = 10, FotoUrl = "u", Disponivel = true, Tipo = TipoProduto.Simples
        };
        _produtos.Setup(r => r.GetByIdAsync(produto.Id, default)).ReturnsAsync(produto);

        var result = await _handler.Handle(produto.Id);

        result.Data!.Tipo.Should().Be("simples");
        result.Data.Itens.Should().BeEmpty();
    }
}
