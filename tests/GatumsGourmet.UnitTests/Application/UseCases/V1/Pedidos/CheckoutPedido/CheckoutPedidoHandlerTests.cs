using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Pedidos.CheckoutPedido;
using GatumsGourmet.Domain.Entities.Carrinho;
using GatumsGourmet.Domain.Entities.Estoque;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Entities.Vendas;
using GatumsGourmet.Domain.Enums.Pedidos;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Pedidos.CheckoutPedido;

public class CheckoutPedidoHandlerTests
{
    private readonly Mock<ICarrinhoRepository> _carrinho = new();
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly Mock<IVendaRepository> _vendas = new();
    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly Mock<IMovimentoEstoqueRepository> _movimentos = new();
    private readonly Mock<IClock> _clock = new();
    private readonly CheckoutPedidoHandler _handler;
    private readonly DateTime _agora = new(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);

    public CheckoutPedidoHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(_agora);
        _handler = new CheckoutPedidoHandler(
            _carrinho.Object, _pedidos.Object, _vendas.Object,
            _produtos.Object, _movimentos.Object, _clock.Object,
            Mock.Of<ILogger<CheckoutPedidoHandler>>());
    }

    [Fact]
    public async Task Handle_CarrinhoVazio_RetornaErro()
    {
        var usuarioId = Guid.NewGuid();
        _carrinho.Setup(r => r.GetByUsuarioAsync(usuarioId, default)).ReturnsAsync(new List<CarrinhoItem>());

        var result = await _handler.Handle(usuarioId, "Cliente");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Carrinho vazio.");
        _pedidos.Verify(r => r.AddAsync(It.IsAny<Pedido>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_CarrinhoComItens_CriaPedidoVendasELimpaCarrinho()
    {
        var usuarioId = Guid.NewGuid();
        var produtoAId = Guid.NewGuid();
        var produtoBId = Guid.NewGuid();
        var itens = new List<CarrinhoItem>
        {
            new() { Id = Guid.NewGuid(), UsuarioId = usuarioId, ProdutoId = produtoAId, Nome = "A", Preco = 10m, FotoUrl = "u", Quantidade = 2 },
            new() { Id = Guid.NewGuid(), UsuarioId = usuarioId, ProdutoId = produtoBId, Nome = "B", Preco = 5m, FotoUrl = "u", Quantidade = 3 }
        };
        _carrinho.Setup(r => r.GetByUsuarioAsync(usuarioId, default)).ReturnsAsync(itens);
        _produtos.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), default))
            .ReturnsAsync(new List<Produto>
            {
                new() { Id = produtoAId, Nome = "A", Estoque = 10, ComboItens = [] },
                new() { Id = produtoBId, Nome = "B", Estoque = 10, ComboItens = [] }
            });
        _movimentos.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<MovimentoEstoque>>(), default)).Returns(Task.CompletedTask);
        Pedido? criado = null;
        _pedidos.Setup(r => r.AddAsync(It.IsAny<Pedido>(), default))
            .Callback<Pedido, CancellationToken>((p, _) => criado = p)
            .Returns(Task.CompletedTask);
        _vendas.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Venda>>(), default)).Returns(Task.CompletedTask);
        _carrinho.Setup(r => r.ClearAsync(usuarioId, default)).Returns(Task.CompletedTask);
        _pedidos.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(usuarioId, "César");

        result.IsSuccess.Should().BeTrue();
        criado.Should().NotBeNull();
        criado!.Total.Should().Be(35m); // 10*2 + 5*3
        criado.StatusAtual.Should().Be(PedidoEtapa.Criado);
        criado.ClienteNome.Should().Be("César");
        criado.Itens.Should().HaveCount(2);
        criado.Timeline.Should().ContainSingle(t => t.Etapa == PedidoEtapa.Criado && t.Por == PedidoActor.Cliente);
        _vendas.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<Venda>>(v => v.Count() == 2), default), Times.Once);
        _carrinho.Verify(r => r.ClearAsync(usuarioId, default), Times.Once);
        _pedidos.Verify(r => r.SaveChangesAsync(default), Times.Once);
        result.Data!.PedidoId.Should().Be(criado.Id);
    }
}

