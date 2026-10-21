using GatumsGourmet.Domain.Interfaces;
using GatumsGourmet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GatumsGourmet.Api.Extensions;

/// <summary>
/// Extensões responsáveis por provisionar o banco de dados na inicialização da aplicação:
/// cria o banco alvo caso não exista, aplica as migrations pendentes e executa o seed inicial.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Garante que o banco exista, aplica as migrations e executa o seed.
    /// Resolve o erro "database does not exist" em ambientes locais (Docker) e em Produção
    /// onde o banco ainda não foi provisionado.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<GatumsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await EnsureDatabaseExistsAsync(db, logger);

        logger.LogInformation("Aplicando migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Migrations aplicadas com sucesso.");

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await DbSeeder.SeedAsync(db, hasher);
    }

    /// <summary>
    /// Conecta ao banco de manutenção 'postgres' e cria o banco alvo caso não exista.
    /// Esta abordagem é necessária porque o EF Core não consegue criar o banco automaticamente
    /// com MigrateAsync quando o banco de dados não existe — o PostgreSQL exige uma conexão
    /// a um banco existente para poder criar outro.
    ///
    /// Em produção (ex.: VPS com PostgreSQL recém-provisionado via Docker), o servidor pode
    /// ainda estar inicializando quando a API sobe. Por isso há retry na conexão de manutenção.
    /// Se, após as tentativas, não for possível garantir que o banco exista, a exceção é
    /// propagada — é melhor falhar com um erro claro do que seguir para MigrateAsync e quebrar
    /// com "database does not exist".
    /// </summary>
    private static async Task EnsureDatabaseExistsAsync(GatumsDbContext db, ILogger logger)
    {
        var connectionString = db.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string não configurada.");

        var csBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        var targetDatabase = csBuilder.Database
            ?? throw new InvalidOperationException("Nome do banco não encontrado na connection string.");

        // Substitui o banco alvo pelo banco de manutenção 'postgres'
        csBuilder.Database = "postgres";
        var maintenanceCs = csBuilder.ToString();

        const int maxTentativas = 10;
        for (var tentativa = 1; tentativa <= maxTentativas; tentativa++)
        {
            try
            {
                await using var conn = new NpgsqlConnection(maintenanceCs);
                await conn.OpenAsync();

                await using var checkCmd = new NpgsqlCommand(
                    "SELECT 1 FROM pg_database WHERE datname = @name", conn);
                checkCmd.Parameters.AddWithValue("name", targetDatabase);
                var exists = await checkCmd.ExecuteScalarAsync();

                if (exists is null)
                {
                    logger.LogInformation("Banco de dados '{Database}' não encontrado. Criando...", targetDatabase);

                    // Nome do banco não pode ser parametrizado; é validado pelo NpgsqlConnectionStringBuilder
                    // e envolvido em aspas duplas com escape para evitar injeção.
                    var safeName = targetDatabase.Replace("\"", "\"\"");
                    await using var createCmd = new NpgsqlCommand(
                        $"CREATE DATABASE \"{safeName}\"", conn);
                    await createCmd.ExecuteNonQueryAsync();
                    logger.LogInformation("Banco de dados '{Database}' criado com sucesso.", targetDatabase);
                }
                else
                {
                    logger.LogInformation("Banco de dados '{Database}' já existe.", targetDatabase);
                }

                return; // sucesso
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == PostgresErrorCodes.DuplicateDatabase)
            {
                // Outra instância criou o banco em paralelo entre o SELECT e o CREATE. Ok.
                logger.LogInformation("Banco de dados '{Database}' já existe (criação concorrente).", targetDatabase);
                return;
            }
            catch (Exception ex) when (tentativa < maxTentativas)
            {
                logger.LogWarning(ex,
                    "Tentativa {Tentativa}/{Max} de garantir o banco '{Database}' falhou. Nova tentativa em 2s...",
                    tentativa, maxTentativas, targetDatabase);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        // Esgotou as tentativas: falha explícita em vez de prosseguir e quebrar no MigrateAsync.
        throw new InvalidOperationException(
            $"Não foi possível garantir a existência do banco '{targetDatabase}' após {maxTentativas} tentativas. " +
            "Verifique a disponibilidade do PostgreSQL e as credenciais de conexão.");
    }
}
