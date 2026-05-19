using FluentValidation;
using GatumsGourmet.Application.UseCases.Auth.GetCurrentUser;
using GatumsGourmet.Application.UseCases.Auth.Login;
using GatumsGourmet.Application.UseCases.Auth.RegisterUser;
using GatumsGourmet.Application.UseCases.Carrinho.AddCarrinhoItem;
using GatumsGourmet.Application.UseCases.Carrinho.ClearCarrinho;
using GatumsGourmet.Application.UseCases.Carrinho.GetCarrinho;
using GatumsGourmet.Application.UseCases.Carrinho.RemoveCarrinhoItem;
using GatumsGourmet.Application.UseCases.Carrinho.UpdateCarrinhoItemQuantidade;
using GatumsGourmet.Application.UseCases.Clientes.CreateCliente;
using GatumsGourmet.Application.UseCases.Clientes.DeleteCliente;
using GatumsGourmet.Application.UseCases.Clientes.ListClientes;
using GatumsGourmet.Application.UseCases.Clientes.UpdateCliente;
using GatumsGourmet.Application.UseCases.Pagamentos.ConfirmarPagamentoPix;
using GatumsGourmet.Application.UseCases.Pagamentos.ConsultarStatusPagamento;
using GatumsGourmet.Application.UseCases.Pagamentos.IniciarPagamento;
using GatumsGourmet.Application.UseCases.Pagamentos.ListOpcoesPagamento;
using GatumsGourmet.Application.UseCases.Pagamentos.ProcessarPagamentoCartao;
using GatumsGourmet.Application.UseCases.Pagamentos.ValidarPagamentoParaAprovacao;
using GatumsGourmet.Application.UseCases.Pedidos.AvancarPedidoEtapa;
using GatumsGourmet.Application.UseCases.Pedidos.CheckoutPedido;
using GatumsGourmet.Application.UseCases.Pedidos.ConcluirPagamentoPedido;
using GatumsGourmet.Application.UseCases.Pedidos.GetPedidoById;
using GatumsGourmet.Application.UseCases.Pedidos.ListPedidos;
using GatumsGourmet.Application.UseCases.Produtos.CreateProduto;
using GatumsGourmet.Application.UseCases.Produtos.DeleteProduto;
using GatumsGourmet.Application.UseCases.Produtos.GetProdutoById;
using GatumsGourmet.Application.UseCases.Produtos.ListCategorias;
using GatumsGourmet.Application.UseCases.Produtos.ListProdutos;
using GatumsGourmet.Application.UseCases.Produtos.UpdateProduto;
using GatumsGourmet.Application.UseCases.Relatorios.GetVendasResumo;
using Microsoft.Extensions.DependencyInjection;

namespace GatumsGourmet.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginValidator>();

        services.AddScoped<ILoginHandler, LoginHandler>();
        services.AddScoped<IRegisterUserHandler, RegisterUserHandler>();
        services.AddScoped<IGetCurrentUserHandler, GetCurrentUserHandler>();

        services.AddScoped<IListProdutosHandler, ListProdutosHandler>();
        services.AddScoped<IGetProdutoByIdHandler, GetProdutoByIdHandler>();
        services.AddScoped<ICreateProdutoHandler, CreateProdutoHandler>();
        services.AddScoped<IUpdateProdutoHandler, UpdateProdutoHandler>();
        services.AddScoped<IDeleteProdutoHandler, DeleteProdutoHandler>();
        services.AddScoped<IListCategoriasHandler, ListCategoriasHandler>();

        services.AddScoped<IListClientesHandler, ListClientesHandler>();
        services.AddScoped<ICreateClienteHandler, CreateClienteHandler>();
        services.AddScoped<IUpdateClienteHandler, UpdateClienteHandler>();
        services.AddScoped<IDeleteClienteHandler, DeleteClienteHandler>();

        services.AddScoped<IGetCarrinhoHandler, GetCarrinhoHandler>();
        services.AddScoped<IAddCarrinhoItemHandler, AddCarrinhoItemHandler>();
        services.AddScoped<IUpdateCarrinhoItemQuantidadeHandler, UpdateCarrinhoItemQuantidadeHandler>();
        services.AddScoped<IRemoveCarrinhoItemHandler, RemoveCarrinhoItemHandler>();
        services.AddScoped<IClearCarrinhoHandler, ClearCarrinhoHandler>();

        services.AddScoped<ICheckoutPedidoHandler, CheckoutPedidoHandler>();
        services.AddScoped<IListPedidosHandler, ListPedidosHandler>();
        services.AddScoped<IGetPedidoByIdHandler, GetPedidoByIdHandler>();
        services.AddScoped<IAvancarPedidoEtapaHandler, AvancarPedidoEtapaHandler>();
        services.AddScoped<IConcluirPagamentoPedidoHandler, ConcluirPagamentoPedidoHandler>();

        services.AddScoped<IListOpcoesPagamentoHandler, ListOpcoesPagamentoHandler>();
        services.AddScoped<IIniciarPagamentoHandler, IniciarPagamentoHandler>();
        services.AddScoped<IProcessarPagamentoCartaoHandler, ProcessarPagamentoCartaoHandler>();
        services.AddScoped<IConsultarStatusPagamentoHandler, ConsultarStatusPagamentoHandler>();
        services.AddScoped<IConfirmarPagamentoPixHandler, ConfirmarPagamentoPixHandler>();
        services.AddScoped<IValidarPagamentoParaAprovacaoHandler, ValidarPagamentoParaAprovacaoHandler>();

        services.AddScoped<IGetVendasResumoHandler, GetVendasResumoHandler>();

        return services;
    }
}
