using Xunit;

namespace GatumsGourmet.IntegrationTests.Infrastructure;

/// <summary>
/// Fixture compartilhada entre todos os testes de integração.
/// Sobe o PostgreSQL via Docker (CliWrapper), cria o WebApplicationFactory,
/// aplica migrations + seed e desce o Docker ao final.
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly CliWrapper _cli = new();

    private const string ComposeFile = "docker-integration.yml";
    private const string ContainerName = "gatums-test-db";
    private const string DbUser = "gatums";

    public const string ConnectionString =
        "Host=localhost;Port=5433;Database=gatums_test;Username=gatums;Password=gatums_test";

    public IntegrationWebAppFactory Factory { get; private set; } = null!;

    // Credenciais seed
    public const string AdminEmail = "admin@gatums.com";
    public const string AdminPassword = "Admin@123";

    public async Task InitializeAsync()
    {
        var composePath = Path.Combine(AppContext.BaseDirectory, ComposeFile);

        // 1. Subir PostgreSQL via Docker Compose
        var up = await _cli.RunAsync("docker", $"compose -f \"{composePath}\" up -d --wait");
        if (!up.Success)
        {
            // Tentar sem --wait (versões antigas do compose)
            var upLegacy = await _cli.RunAsync("docker", $"compose -f \"{composePath}\" up -d");
            if (!upLegacy.Success)
                throw new InvalidOperationException(
                    $"Falha ao subir Docker. Verifique se o Docker está rodando.\n{up.Output}");
        }

        // 2. Aguardar PostgreSQL estar pronto
        await WaitForPostgresAsync();

        // 3. Criar WebApplicationFactory (roda Program.cs → MigrateAsync + Seed)
        Factory = new IntegrationWebAppFactory(ConnectionString);
        Factory.CreateClient(); // força inicialização do servidor

        // 4. Reset para garantir estado limpo
        await Factory.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();

        var composePath = Path.Combine(AppContext.BaseDirectory, ComposeFile);
        await _cli.RunAsync("docker", $"compose -f \"{composePath}\" down -v");
    }

    private async Task WaitForPostgresAsync()
    {
        for (var i = 0; i < 30; i++)
        {
            var result = await _cli.RunAsync(
                "docker", $"exec {ContainerName} pg_isready -U {DbUser}");

            if (result.Success)
                return;

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            "PostgreSQL não ficou pronto em 30 segundos. " +
            $"Verifique: docker logs {ContainerName}");
    }
}

/// <summary>
/// Coleção xUnit que compartilha a IntegrationTestFixture entre todas as classes de teste.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture> { }
