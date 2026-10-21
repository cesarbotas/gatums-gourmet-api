using GatumsGourmet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Factory;

public class DockerIntegrationFixture : IAsyncLifetime
{
    private readonly string _composeFilePath;

    // Connection string apontando para o banco de testes na porta 5433
    public const string ConnectionString =
        "Host=localhost;Port=5433;Database=gatums_test;Username=gatums;Password=gatums_test";

    // Connection string para o banco padrão 'postgres' (necessário para criar o banco de testes)
    private const string MaintenanceConnectionString =
        "Host=localhost;Port=5433;Database=postgres;Username=gatums;Password=gatums_test";

    public DockerIntegrationFixture()
    {
        var solutionDir = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName;
        _composeFilePath = Path.Combine(solutionDir ?? ".", "docker-integration.yml");
    }

    public async Task InitializeAsync()
    {
        // Aguarda o banco ficar saudável
        await WaitForDatabaseAsync();

        // Garante que o database existe e aplica as migrations
        await RunMigrationsAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Cria o banco de dados caso não exista e aplica todas as migrations do projeto Infrastructure.
    /// Resolve o erro "database does not exist" tanto localmente quanto em ambientes de produção
    /// onde o banco não foi provisionado previamente.
    /// </summary>
    public async Task RunMigrationsAsync()
    {
        // 1. Criar o banco de dados se não existir (conecta ao 'postgres' como maintenance db)
        await EnsureDatabaseExistsAsync();

        // 2. Aplicar as migrations via EF Core
        var options = new DbContextOptionsBuilder<GatumsDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var db = new GatumsDbContext(options);
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// Conecta ao banco de manutenção 'postgres' e cria o banco alvo caso não exista.
    /// Equivalente a executar: CREATE DATABASE gatums_test — IF NOT EXISTS.
    /// </summary>
    private async Task EnsureDatabaseExistsAsync()
    {
        // Extrai o nome do banco da connection string
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString);
        var databaseName = builder.Database
            ?? throw new InvalidOperationException("Database name não encontrado na ConnectionString.");

        await using var conn = new NpgsqlConnection(MaintenanceConnectionString);
        await conn.OpenAsync();

        // Verifica se o banco já existe
        await using var checkCmd = new NpgsqlCommand(
            $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'", conn);
        var exists = await checkCmd.ExecuteScalarAsync();

        if (exists is null)
        {
            await using var createCmd = new NpgsqlCommand(
                $"CREATE DATABASE \"{databaseName}\"", conn);
            await createCmd.ExecuteNonQueryAsync();
        }
    }

    private async Task WaitForDatabaseAsync()
    {
        for (var i = 0; i < 30; i++)
        {
            try
            {
                await using var conn = new NpgsqlConnection(MaintenanceConnectionString);
                await conn.OpenAsync();
                return; // Conexão bem-sucedida — banco está pronto
            }
            catch
            {
                // Servidor ainda não está pronto, aguarda e tenta novamente
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            "O banco de dados de teste não ficou saudável em 30 segundos. " +
            "Verifique se o Docker está rodando com: docker compose -f docker-integration.yml up -d");
    }
}

[CollectionDefinition("Database Collection")]
public class DatabaseCollection : ICollectionFixture<DockerIntegrationFixture>
{
    // Esta classe serve apenas como âncora para o xUnit compartilhar a fixture entre testes
}