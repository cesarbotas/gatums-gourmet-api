using System.Text;
using CliWrap;
using CliWrap.Buffered;
using GatumsGourmet.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Factory;

/// <summary>
/// Factory de testes de integração. Sobe a dependência (PostgreSQL) via docker compose
/// usando CliWrap, aguarda o banco ficar saudável e levanta a API real através da
/// <see cref="WebApplicationFactory{TEntryPoint}"/>. Ao final, derruba os containers.
///
/// A connection string de teste (porta 5433) é injetada na configuração, então a própria
/// API cria o banco e aplica as migrations no startup (InitializeDatabaseAsync).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _composeFilePath;
    private readonly string _connectionString;
    private readonly ILogger<CustomWebApplicationFactory> _logger;

    public CustomWebApplicationFactory()
    {
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<CustomWebApplicationFactory>();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada em appsettings.Test.json.");

        // Guarda de segurança: os testes DEVEM usar o banco de teste (sufixo _test).
        // Impede rodar a suíte acidentalmente contra o banco 'gatums' (produção/dev),
        // que agora coexiste no mesmo container de teste.
        var dbName = new NpgsqlConnectionStringBuilder(_connectionString).Database;
        if (string.IsNullOrWhiteSpace(dbName) || !dbName.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Connection string de teste deve apontar para um banco '*_test' (atual: '{dbName}'). " +
                "Verifique ConnectionStrings:DefaultConnection em appsettings.Test.json.");

        // Publica as chaves de teste como variáveis de ambiente ANTES do host subir.
        // Variáveis de ambiente têm precedência sobre o appsettings.json da API e são
        // lidas por WebApplication.CreateBuilder desde o início — garantindo que tanto
        // o gerador de token (assinatura) quanto o middleware (validação) usem o MESMO
        // Jwt:Secret. Sem isso, a validação pegaria o secret do appsettings.json da API
        // ("The signature key was not found").
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _connectionString);
        Environment.SetEnvironmentVariable("Jwt__Secret", configuration["Jwt:Secret"]);
        Environment.SetEnvironmentVariable("Jwt__Issuer", configuration["Jwt:Issuer"]);
        Environment.SetEnvironmentVariable("Jwt__Audience", configuration["Jwt:Audience"]);

        // docker-integration.yml fica na raiz do projeto de teste; sobe cinco níveis a partir do bin.
        // bin/Debug/net10.0 -> net10.0 / Debug / bin / <projeto> / docker-integration.yml
        _composeFilePath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "docker-integration.yml"));

        if (!File.Exists(_composeFilePath))
            throw new FileNotFoundException($"docker-compose não encontrado: {_composeFilePath}");
    }

    public async Task InitializeAsync()
    {
        // NOTA: o docker compose up foi desativado. O container Postgres de teste é mantido
        // rodando manualmente na máquina (docker compose -f docker-integration.yml up -d),
        // evitando a instabilidade do Docker Desktop ao subir/derrubar a cada execução.
        // Para reativar o gerenciamento automático, descomente o bloco abaixo.
        //
        // var stdErr = new StringBuilder();
        // try
        // {
        //     _logger.LogInformation("[Init] docker compose up -d ...");
        //     var result = await Cli.Wrap("docker")
        //         .WithArguments(new[] { "compose", "-f", _composeFilePath, "up", "-d" })
        //         .WithValidation(CommandResultValidation.None)
        //         .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
        //         .ExecuteBufferedAsync();
        //
        //     if (result.ExitCode != 0)
        //         throw new InvalidOperationException($"docker compose up falhou (exit {result.ExitCode}):\n{stdErr}");
        //
        //     await WaitForDatabaseAsync();
        //     _logger.LogInformation("[Init] PostgreSQL de teste está pronto.");
        // }
        // catch (Exception ex)
        // {
        //     throw new InvalidOperationException(
        //         $"[Init] Falha ao subir docker compose.\nSTDERR:\n{stdErr}", ex);
        // }

        // Apenas aguarda o banco (já em execução) aceitar conexões.
        await WaitForDatabaseAsync();
        _logger.LogInformation("[Init] PostgreSQL de teste está pronto.");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(l => { l.ClearProviders(); l.AddConsole(); l.SetMinimumLevel(LogLevel.Information); });
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false)
               .AddEnvironmentVariables();

            // Garante que a API use o banco de teste (porta 5433).
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString
            });
        });

        // Reforço à prova de precedência de configuração: o appsettings.json da API
        // (content root) pode sobrescrever o appsettings.Test.json em algumas ordens de
        // carga, fazendo o DbContext apontar para o banco de produção ('gatums'). Aqui
        // removemos o registro do DbContext e o re-registramos explicitamente com a
        // connection string de teste. ConfigureTestServices roda por último, então vence.
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<GatumsDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<GatumsDbContext>(opt => opt.UseNpgsql(_connectionString));
        });
    }

    /// <summary>
    /// Conecta ao banco de manutenção 'postgres' até o servidor aceitar conexões
    /// (ou estoura timeout após ~30s).
    /// </summary>
    private async Task WaitForDatabaseAsync()
    {
        var maintenanceBuilder = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            Database = "postgres"
        };
        var maintenanceCs = maintenanceBuilder.ToString();

        const int maxTentativas = 60;
        for (var i = 0; i < maxTentativas; i++)
        {
            try
            {
                await using var conn = new NpgsqlConnection(maintenanceCs);
                await conn.OpenAsync();
                return;
            }
            catch
            {
                // Servidor ainda subindo — aguarda e tenta de novo.
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            $"O PostgreSQL de teste não ficou saudável em {maxTentativas} segundos. " +
            "Verifique se o Docker está rodando.");
    }

    // IAsyncLifetime (xUnit v2) exige DisposeAsync() retornando Task.
    // Implementado explicitamente para não colidir com WebApplicationFactory.DisposeAsync (ValueTask).
    async Task IAsyncLifetime.DisposeAsync()
    {
        // NOTA: o docker compose down foi desativado. O container Postgres de teste é mantido
        // rodando manualmente na máquina entre execuções. Para reativar o teardown automático,
        // descomente o bloco abaixo.
        //
        // try
        // {
        //     _logger.LogInformation("[Dispose] docker compose down ...");
        //     await Cli.Wrap("docker")
        //         .WithArguments(new[] { "compose", "-f", _composeFilePath, "down" })
        //         .WithValidation(CommandResultValidation.None)
        //         .ExecuteBufferedAsync();
        // }
        // catch (Exception ex)
        // {
        //     _logger.LogError(ex, "[Dispose] Falha ao executar docker compose down.");
        // }

        await base.DisposeAsync();
    }
}
