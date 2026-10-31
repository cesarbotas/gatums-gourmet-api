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

        // Storage de imagens (S3-compatível): a seção Storage muda por ambiente
        // (MinIO local / Supabase hml / R2 prd). Registro condicional — só há um
        // cliente S3 de verdade quando o endpoint está configurado; sem config,
        // um fallback explícito evita 500 opaco/NRE no upload.
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        var storageOptions = configuration.GetSection("Storage").Get<StorageOptions>();

        if (!string.IsNullOrWhiteSpace(storageOptions?.ServiceUrl))
        {
            services.AddSingleton<IAmazonS3>(_ =>
            {
                var s3Config = new AmazonS3Config
                {
                    ServiceURL = storageOptions.ServiceUrl,
                    ForcePathStyle = storageOptions.ForcePathStyle,
                    AuthenticationRegion = storageOptions.Region,
                    // A V4 do SDK liga checksums CRC + aws-chunked por padrão, o
                    // que provedores S3-compatíveis (MinIO) rejeitam sobre HTTP.
                    // WhenRequired volta ao comportamento clássico (sem checksum
                    // extra), mantendo a assinatura SigV4 e funcionando em http://.
                    RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
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

