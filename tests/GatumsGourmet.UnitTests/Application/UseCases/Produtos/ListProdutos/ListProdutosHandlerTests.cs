using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.Produtos.ListProdutos;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Enums.Produtos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Produtos.ListProdutos;

public class ListProdutosHandlerTests
{
    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly ListProdutosHandler _handler;

    public ListProdutosHandlerTests() => _handler = new ListProdutosHandler(_produtos.Object);

    private static Categoria Cat(long id = 1, string nome = "Cat") => new() { Id = id, Nome = nome, Ativo = true };

    private static List<Produto> CreateProdutos(int count) => Enumerable.Range(0, count)
        .Select(i => new Produto
        {
            Id = Guid.NewGuid(), Nome = $"Produto {i}", Descricao = "desc", CategoriaId = 1, Categoria = Cat(),
            Preco = 10m * i, FotoUrl = "url", Disponivel = true, Tipo = TipoProduto.Simples
        }).ToList();

    [Fact]
    public async Task Handle_SemFiltros_RetornaPaginado()
    {
        var items = CreateProdutos(3);
        _produtos.Setup(r => r.ListAsync(1, 10, null, null, null, null, default))
            .ReturnsAsync((items, 3));

        var result = await _handler.Handle(new ListProdutosRequest(1, 10, null, null, null, null));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Itens.Should().HaveCount(3);
        result.Data.TotalRegistros.Should().Be(3);
        result.Data.Itens[0].CategoriaNome.Should().Be("Cat");
    }

    [Fact]
    public async Task Handle_Vazio_RetornaListaVazia()
    {
        _produtos.Setup(r => r.ListAsync(1, 10, null, null, null, null, default))
            .ReturnsAsync((new List<Produto>(), 0));

        var result = await _handler.Handle(new ListProdutosRequest());

        result.IsSuccess.Should().BeTrue();
        result.Data!.Itens.Should().BeEmpty();
        result.Data.TotalRegistros.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ComFiltroCategoria_PassaFiltroParaRepositorio()
    {
        var items = CreateProdutos(1);
        _produtos.Setup(r => r.ListAsync(1, 10, null, 5L, null, null, default))
            .ReturnsAsync((items, 1));

        var result = await _handler.Handle(new ListProdutosRequest(1, 10, null, 5L, null, null));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Itens.Should().HaveCount(1);
        _produtos.Verify(r => r.ListAsync(1, 10, null, 5L, null, null, default), Times.Once);
    }

    [Fact]
    public async Task Handle_TipoConvertidoParaLowercase()
    {
        var items = new List<Produto>
        {
            new() { Id = Guid.NewGuid(), Nome = "Combo", Descricao = "d", CategoriaId = 1, Categoria = Cat(), Preco = 50, FotoUrl = "u", Disponivel = true, Tipo = TipoProduto.Combo }
        };
        _produtos.Setup(r => r.ListAsync(1, 10, null, null, null, null, default))
            .ReturnsAsync((items, 1));

        var result = await _handler.Handle(new ListProdutosRequest());

        result.Data!.Itens.First().Tipo.Should().Be("combo");
    }
}

