using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Carrinho.AddCarrinhoItem;
using GatumsGourmet.Domain.Entities.Carrinho;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Enums.Produtos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Carrinho.AddCarrinhoItem;

public class AddCarrinhoItemHandlerTests
{
    private readonly Mock<ICarrinhoRepository> _carrinho = new();
    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly AddCarrinhoItemHandler _handler;

    public AddCarrinhoItemHandlerTests() => _handler = new AddCarrinhoItemHandler(_carrinho.Object, _produtos.Object);

    private static Produto CreateProduto(Guid id) => new()
    {
        Id = id, Nome = "Pizza", Descricao = "d", Categoria = "C",
        Preco = 40m, FotoUrl = "foto", Disponivel = true, Tipo = TipoProduto.Simples
    };

    [Fact]
    public async Task Handle_ProdutoInexistente_RetornaErro()
    {
        _produtos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Produto?)null);

        var result = await _handler.Handle(Guid.NewGuid(), new AddCarrinhoItemRequest(Guid.NewGuid(), 1));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Produto não encontrado.");
        _carrinho.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Handle_ItemNovo_AdicionaComSnapshotDoProduto()
    {
        var usuarioId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        var produto = CreateProduto(produtoId);
        _produtos.Setup(r => r.GetByIdAsync(produtoId, default)).ReturnsAsync(produto);
        _carrinho.Setup(r => r.GetItemAsync(usuarioId, produtoId, default)).ReturnsAsync((CarrinhoItem?)null);
        CarrinhoItem? adicionado = null;
        _carrinho.Setup(r => r.AddAsync(It.IsAny<CarrinhoItem>(), default))
            .Callback<CarrinhoItem, CancellationToken>((i, _) => adicionado = i)
            .Returns(Task.CompletedTask);
        _carrinho.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(usuarioId, new AddCarrinhoItemRequest(produtoId, 3));

        result.IsSuccess.Should().BeTrue();
        adicionado.Should().NotBeNull();
        adicionado!.Quantidade.Should().Be(3);
        adicionado.Nome.Should().Be("Pizza");
        adicionado.Preco.Should().Be(40m);
        adicionado.FotoUrl.Should().Be("foto");
    }

    [Fact]
    public async Task Handle_ItemExistente_SomaQuantidade()
    {
        var usuarioId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        var produto = CreateProduto(produtoId);
        var existente = new CarrinhoItem { Id = Guid.NewGuid(), UsuarioId = usuarioId, ProdutoId = produtoId, Nome = "Pizza", Preco = 40m, FotoUrl = "foto", Quantidade = 2 };
        _produtos.Setup(r => r.GetByIdAsync(produtoId, default)).ReturnsAsync(produto);
        _carrinho.Setup(r => r.GetItemAsync(usuarioId, produtoId, default)).ReturnsAsync(existente);
        _carrinho.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(usuarioId, new AddCarrinhoItemRequest(produtoId, 3));

        result.IsSuccess.Should().BeTrue();
        existente.Quantidade.Should().Be(5);
        _carrinho.Verify(r => r.AddAsync(It.IsAny<CarrinhoItem>(), default), Times.Never);
    }
}
