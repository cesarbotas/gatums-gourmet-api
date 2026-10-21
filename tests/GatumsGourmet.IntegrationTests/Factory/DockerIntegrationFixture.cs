using CliWrap;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Factory;

public class DockerIntegrationFixture : IAsyncLifetime
{
    private readonly string _composeFilePath;

    public DockerIntegrationFixture()
    {
        // Localiza o caminho do arquivo docker-compose de teste (ajuste se necessário para apontar para a raiz ou pasta docker)
        var solutionDir = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName;
        _composeFilePath = Path.Combine(solutionDir ?? ".", "docker-integration.yml");
    }

    public async Task InitializeAsync()
    {
        // Executa: docker compose -f docker-compose.test.yml up -d
/*         await Cli.Wrap("docker")
            .WithArguments(args => args
                .Add("compose")
                .Add("-f").Add(_composeFilePath)
                .Add("up")
                .Add("-d"))
            .ExecuteAsync(); */

        // Aguarda o banco ficar saudável (opcional, mas recomendado)
        await WaitForDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        // Executa: docker compose -f docker-compose.test.yml down -v
/*         await Cli.Wrap("docker")
            .WithArguments(args => args
                .Add("compose")
                .Add("-f").Add(_composeFilePath)
                .Add("down")
                .Add("-v"))
            .ExecuteAsync(); */
    }

    private async Task<bool> WaitForDatabaseAsync()
    {
        for (int i = 0; i < 20; i++)
        {
            try
            {
/*                 var result = await Cli.Wrap("docker")
                    .WithArguments(args => args
                        .Add("inspect")
                        .Add("--format")
                        .Add("{{.State.Health.Status}}")
                        .Add("gatums-test-db"))
                    .ExecuteBufferedAsync(); */

                /* if (result.StandardOutput.Trim() == "healthy")
                    return true; */
            }
            catch
            {
                // Container ainda não subiu, ignora e tenta novamente
            }

            await Task.Delay(1000);
        }
        throw new TimeoutException("O banco de dados de teste não ficou saudável a tempo.");
    }
}

[CollectionDefinition("Database Collection")]
public class DatabaseCollection : ICollectionFixture<DockerIntegrationFixture>
{
    // Esta classe serve apenas como âncora para o xUnit compartilhar a fixture entre testes
}