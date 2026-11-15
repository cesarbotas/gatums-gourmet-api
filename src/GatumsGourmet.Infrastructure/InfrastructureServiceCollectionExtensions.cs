using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Services;
using GatumsGourmet.Infrastructure.Data;
using GatumsGourmet.Infrastructure.Data.Repositories.Auth;
using GatumsGourmet.Infrastructure.Data.Repositories.Carrinho;
using GatumsGourmet.Infrastructure.Data.Repositories.Clientes;
using GatumsGourmet.Infrastructure.Data.Repositories.Estoque;
using GatumsGourmet.Infrastructure.Data.Repositories.Pagamentos;
using GatumsGourmet.Infrastructure.Data.Repositories.Pedidos;
using GatumsGourmet.Infrastructure.Data.Repositories.Produtos;
using GatumsGourmet.Infrastructure.Data.Repositories.Vendas;
using GatumsGourmet.Infrastructure.Services;
using GatumsGourmet.Infrastructure.Services.Auth;
using GatumsGourmet.Infrastructure.Services.Enderecos;
using GatumsGourmet.Infrastructure.Services.Storage;
using GatumsGourmet.Infrastructure.Services.Pagamentos;
using Amazon.Runtime;
using Amazon.S3;
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
        services.AddScoped<IMovimentoEstoqueRepository, MovimentoEstoqueRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IPermissionResolver, PermissionResolver>();
        services.AddScoped<IPedidoWorkflowService, PedidoWorkflowService>();

        services.AddScoped<IPaymentGateway, MockPaymentGateway>();

        // Provedor de PIX — Mercado Pago quando AccessToken estiver configurado; fallback para MockPixProvider em desenvolvimento/testes
        var mpAccessToken = configuration["MercadoPago:AccessToken"];
        if (!string.IsNullOrWhiteSpace(mpAccessToken))
        {
            var mpBaseUrl = configuration["MercadoPago:BaseUrl"] ?? "https://api.mercadopago.com/";
            services.AddHttpClient<IPixProvider, MercadoPagoPixProvider>(client =>
            {
                client.BaseAddress = new Uri(mpBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }
        else
        {
            services.AddScoped<IPixProvider, MockPixProvider>();
        }

        // Provedor de Pix via Orders API (V2), em paralelo ao IPixProvider acima.
        if (!string.IsNullOrWhiteSpace(mpAccessToken))
        {
            var mpOrdersBaseUrl = configuration["MercadoPago:BaseUrl"] ?? "https://api.mercadopago.com/";
            services.AddHttpClient<IPixOrdersProvider, MercadoPagoOrdersPixProvider>(client =>
            {
                client.BaseAddress = new Uri(mpOrdersBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }
        else
        {
            services.AddScoped<IPixOrdersProvider, MockPixOrdersProvider>();
        }

        // Validador da assinatura (x-signature) do webhook de orders do Mercado Pago.
        services.AddSingleton<IMercadoPagoWebhookSignatureValidator, MercadoPagoWebhookSignatureValidator>();

        // Provedor de CEP (ViaCEP) — primeira integração HTTP de saída do projeto.
        var viaCepBaseUrl = configuration["ViaCep:BaseUrl"] ?? "https://viacep.com.br/ws/";
        services.AddHttpClient<ICepProvider, ViaCepProvider>(client =>
        {
            client.BaseAddress = new Uri(viaCepBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        // Storage de imagens (S3-compatível): a seção Storage muda por ambiente
        // (MinIO local / Supabase hml / R2 prd). Registro condicional — só há um
        // cliente S3 de verdade quando o endpoint está configurado; sem config,
        // um fallback explícito evita 500 opaco/NRE no upload.
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        var storageOptions = configuration.GetSection("Storage").Get<StorageOptions>();

        var storageConfigurado = !string.IsNullOrWhiteSpace(storageOptions?.ServiceUrl);

        if (storageConfigurado)
        {
            services.AddSingleton<IAmazonS3>(_ =>
            {
                var s3Config = new AmazonS3Config
                {
                    ServiceURL = storageOptions.ServiceUrl,
                    ForcePathStyle = storageOptions.ForcePathStyle,
                    //RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(storageOptions.Region),
                    AuthenticationRegion = storageOptions.Region,
                    // HTTP apenas quando o endpoint for http:// (MinIO local).
                    // Supabase/R2 usam HTTPS — UseHttp=true quebraria a conexão
                    // (Forbidden/connection refused sobre HTTPS forçado em HTTP).
                    UseHttp = storageOptions.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
                    SignatureVersion = "4"
                };

                return new AmazonS3Client(
                    new BasicAWSCredentials(storageOptions.AccessKey, storageOptions.SecretKey),
                    s3Config);
            });
            services.AddScoped<IImageStorage, S3ImageStorage>();
        }
        else
        {
            services.AddScoped<IImageStorage, UnconfiguredImageStorage>();
        }

        return services;
    }
}

