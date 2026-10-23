using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Auth.Login;
using GatumsGourmet.Application.UseCases.V1.Auth.RegisterUser;
using GatumsGourmet.Application.UseCases.V1.Carrinho.AddCarrinhoItem;
using GatumsGourmet.Application.UseCases.V1.Clientes.CreateCliente;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPagamento;
using GatumsGourmet.Application.UseCases.V1.Produtos.CreateProduto;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Produtos;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.Validators;

public class ValidatorsTests
{
    // ---- LoginValidator ----
    [Fact]
    public void LoginValidator_EmailInvalido_Falha()
    {
        var r = new LoginValidator().Validate(new LoginRequest("nao-email", ""));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LoginValidator_Valido_Passa()
    {
        var r = new LoginValidator().Validate(new LoginRequest("a@b.com", "senha"));
        r.IsValid.Should().BeTrue();
    }

    // ---- RegisterUserValidator ----
    [Fact]
    public void RegisterUserValidator_SenhaCurta_Falha()
    {
        var r = new RegisterUserValidator().Validate(new RegisterUserRequest("Nome", "a@b.com", "123", null));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterUserValidator_Valido_Passa()
    {
        var r = new RegisterUserValidator().Validate(new RegisterUserRequest("Nome", "a@b.com", "senha123", "11999"));
        r.IsValid.Should().BeTrue();
    }

    // ---- CreateProdutoValidator ----
    [Fact]
    public void CreateProdutoValidator_PrecoZero_Falha()
    {
        var req = new CreateProdutoRequest("Nome", "d", 1, 0m, "url", true, TipoProduto.Simples, null);
        var r = new CreateProdutoValidator().Validate(req);
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateProdutoValidator_Valido_Passa()
    {
        var req = new CreateProdutoRequest("Nome", "d", 1, 10m, "url", true, TipoProduto.Simples, null);
        var r = new CreateProdutoValidator().Validate(req);
        r.IsValid.Should().BeTrue();
    }

    // ---- CreateClienteValidator ----
    [Fact]
    public void CreateClienteValidator_EstadoComTamanhoErrado_Falha()
    {
        var req = new CreateClienteRequest("Nome", "a@b.com", null,
            new EnderecoRequest("Rua", "1", null, "Bairro", "Cidade", "SPP", "01000000"));
        var r = new CreateClienteValidator().Validate(req);
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateClienteValidator_Valido_Passa()
    {
        var req = new CreateClienteRequest("Nome", "a@b.com", "119",
            new EnderecoRequest("Rua", "1", "Ap", "Bairro", "Cidade", "SP", "01000000"));
        var r = new CreateClienteValidator().Validate(req);
        r.IsValid.Should().BeTrue();
    }

    // ---- AddCarrinhoItemValidator ----
    [Fact]
    public void AddCarrinhoItemValidator_QuantidadeZero_Falha()
    {
        var r = new AddCarrinhoItemValidator().Validate(new AddCarrinhoItemRequest(Guid.NewGuid(), 0));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddCarrinhoItemValidator_Valido_Passa()
    {
        var r = new AddCarrinhoItemValidator().Validate(new AddCarrinhoItemRequest(Guid.NewGuid(), 2));
        r.IsValid.Should().BeTrue();
    }

    // ---- IniciarPagamentoValidator ----
    [Fact]
    public void IniciarPagamentoValidator_ValorZero_Falha()
    {
        var r = new IniciarPagamentoValidator().Validate(new IniciarPagamentoRequest(Guid.NewGuid(), MetodoPagamento.Pix, 0m));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IniciarPagamentoValidator_PedidoVazio_Falha()
    {
        var r = new IniciarPagamentoValidator().Validate(new IniciarPagamentoRequest(Guid.Empty, MetodoPagamento.Pix, 10m));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IniciarPagamentoValidator_Valido_Passa()
    {
        var r = new IniciarPagamentoValidator().Validate(new IniciarPagamentoRequest(Guid.NewGuid(), MetodoPagamento.Pix, 10m));
        r.IsValid.Should().BeTrue();
    }
}
