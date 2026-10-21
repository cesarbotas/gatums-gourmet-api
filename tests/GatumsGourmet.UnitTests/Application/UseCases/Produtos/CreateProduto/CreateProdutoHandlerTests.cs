using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Produtos.CreateProduto;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Enums.Produtos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Interfaces;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Produtos.CreateProduto;

public class CreateProdutoHandlerTests
{
    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly Mock<IClock> _clock = new();
    private readonly CreateProdutoHandler _handler;

    public CreateProdutoHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        _handler = new CreateProdutoHandler(_produtos.Object, _clock.Object);
    }

    [Fact]
    public async Task Handle_ProdutoSimples_CriaERetornaId()
    {
        _produtos.Setup(r => r.AddAsync(It.IsAny<Produto>(), default)).Returns(Task.CompletedTask);
        _produtos.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var request = new CreateProdutoRequest("Pizza", "desc", "Pizzas", 45.90m, "url", true, TipoProduto.Simples, null);

        var result = await _handler.Handle(request);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().NotBeEmpty();
        _produtos.Verify(r => r.AddAsync(It.Is<Produto>(p => p.Nome == "Pizza" && p.Tipo == TipoProduto.Simples), default), Times.Once);
    }

    [Fact]
    public async Task Handle_ComboSemItens_LancaBusinessRuleException()
    {
        var request = new CreateProdutoRequest("Combo", "desc", "C", 89.90m, "url", true, TipoProduto.Combo, null);

        var act = () => _handler.Handle(request);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*item*");
    }

    [Fact]
    public async Task Handle_ComboComItens_CriaProdutoEItens()
    {
        _produtos.Setup(r => r.AddAsync(It.IsAny<Produto>(), default)).Returns(Task.CompletedTask);
        _produtos.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var itens = new List<ComboItemInput> { new(Guid.NewGuid(), 2) };
        var request = new CreateProdutoRequest("Combo", "desc", "C", 89.90m, "url", true, TipoProduto.Combo, itens);

        var result = await _handler.Handle(request);

        result.IsSuccess.Should().BeTrue();
        _produtos.Verify(r => r.AddAsync(It.Is<Produto>(p => p.ComboItens.Count == 1), default), Times.Once);
    }
}
