using FluentValidation;
using GatumsGourmet.Application.UseCases.V1.Auth.GetCurrentUser;
using GatumsGourmet.Application.UseCases.V1.Auth.Login;
using GatumsGourmet.Application.UseCases.V1.Auth.Logout;
using GatumsGourmet.Application.UseCases.V1.Auth.LogoutAll;
using GatumsGourmet.Application.UseCases.V1.Auth.RefreshToken;
using GatumsGourmet.Application.UseCases.V1.Auth.RegisterUser;
using GatumsGourmet.Application.UseCases.V1.Carrinho.AddCarrinhoItem;
using GatumsGourmet.Application.UseCases.V1.Carrinho.ClearCarrinho;
using GatumsGourmet.Application.UseCases.V1.Carrinho.GetCarrinho;
using GatumsGourmet.Application.UseCases.V1.Carrinho.RemoveCarrinhoItem;
using GatumsGourmet.Application.UseCases.V1.Carrinho.UpdateCarrinhoItemQuantidade;
using GatumsGourmet.Application.UseCases.V1.Clientes.CreateCliente;
using GatumsGourmet.Application.UseCases.V1.Clientes.DeleteCliente;
using GatumsGourmet.Application.UseCases.V1.Clientes.ListClientes;
using GatumsGourmet.Application.UseCases.V1.Clientes.UpdateCliente;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ConfirmarPagamentoPix;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ConsultarStatusPagamento;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPagamento;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ListOpcoesPagamento;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarPagamentoCartao;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ValidarPagamentoParaAprovacao;
using GatumsGourmet.Application.UseCases.V1.Pedidos.AvancarPedidoEtapa;
using GatumsGourmet.Application.UseCases.V1.Pedidos.CheckoutPedido;
using GatumsGourmet.Application.UseCases.V1.Pedidos.ConcluirPagamentoPedido;
using GatumsGourmet.Application.UseCases.V1.Pedidos.GetPedidoById;
using GatumsGourmet.Application.UseCases.V1.Pedidos.ListPedidos;
using GatumsGourmet.Application.UseCases.V1.Categorias.CreateCategoria;
using GatumsGourmet.Application.UseCases.V1.Categorias.DeleteCategoria;
using GatumsGourmet.Application.UseCases.V1.Categorias.GetCategoriaById;
using GatumsGourmet.Application.UseCases.V1.Categorias.ListCategorias;
using GatumsGourmet.Application.UseCases.V1.Categorias.UpdateCategoria;
using GatumsGourmet.Application.UseCases.V1.Produtos.CreateProduto;
using GatumsGourmet.Application.UseCases.V1.Produtos.DeleteProduto;
using GatumsGourmet.Application.UseCases.V1.Produtos.GetProdutoById;
using GatumsGourmet.Application.UseCases.V1.Produtos.ListProdutos;
using GatumsGourmet.Application.UseCases.V1.Produtos.UpdateProduto;
using GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasResumo;
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
        services.AddScoped<IRefreshTokenHandler, RefreshTokenHandler>();
        services.AddScoped<ILogoutHandler, LogoutHandler>();
        services.AddScoped<ILogoutAllHandler, LogoutAllHandler>();

        services.AddScoped<IListProdutosHandler, ListProdutosHandler>();
        services.AddScoped<IGetProdutoByIdHandler, GetProdutoByIdHandler>();
        services.AddScoped<ICreateProdutoHandler, CreateProdutoHandler>();
        services.AddScoped<IUpdateProdutoHandler, UpdateProdutoHandler>();
        services.AddScoped<IDeleteProdutoHandler, DeleteProdutoHandler>();

        services.AddScoped<IListCategoriasHandler, ListCategoriasHandler>();
        services.AddScoped<IGetCategoriaByIdHandler, GetCategoriaByIdHandler>();
        services.AddScoped<ICreateCategoriaHandler, CreateCategoriaHandler>();
        services.AddScoped<IUpdateCategoriaHandler, UpdateCategoriaHandler>();
        services.AddScoped<IDeleteCategoriaHandler, DeleteCategoriaHandler>();

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
