using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Services;
using GatumsGourmet.Infrastructure.Data;
using GatumsGourmet.Infrastructure.Data.Repositories.Auth;
using GatumsGourmet.Infrastructure.Data.Repositories.Carrinho;
using GatumsGourmet.Infrastructure.Data.Repositories.Clientes;
using GatumsGourmet.Infrastructure.Data.Repositories.Pagamentos;
using GatumsGourmet.Infrastructure.Data.Repositories.Pedidos;
using GatumsGourmet.Infrastructure.Data.Repositories.Produtos;
using GatumsGourmet.Infrastructure.Data.Repositories.Vendas;
using GatumsGourmet.Infrastructure.Services;
using GatumsGourmet.Infrastructure.Services.Auth;
using GatumsGourmet.Infrastructure.Services.Enderecos;
using GatumsGourmet.Infrastructure.Services.Pagamentos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GatumsGourmet.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GatumsDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<ICarrinhoRepository, CarrinhoRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<ITransacaoRepository, TransacaoRepository>();
        services.AddScoped<IVendaRepository, VendaRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IPermissionResolver, PermissionResolver>();
        services.AddScoped<IPedidoWorkflowService, PedidoWorkflowService>();

        services.AddScoped<IPaymentGateway, MockPaymentGateway>();
        services.AddScoped<IPixProvider, MockPixProvider>();

        // Provedor de CEP (ViaCEP) — primeira integração HTTP de saída do projeto.
        var viaCepBaseUrl = configuration["ViaCep:BaseUrl"] ?? "https://viacep.com.br/ws/";
        services.AddHttpClient<ICepProvider, ViaCepProvider>(client =>
        {
            client.BaseAddress = new Uri(viaCepBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }
}

