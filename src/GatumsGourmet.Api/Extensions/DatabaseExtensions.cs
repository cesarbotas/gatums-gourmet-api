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

        // Se o banco alvo já responde, não há o que provisionar: pula a etapa de manutenção.
        // Essencial para Postgres gerenciado (Neon, Supabase, RDS), que já vem com o banco
        // criado e frequentemente NÃO permite conectar no database 'postgres' nem criar bancos.
        if (await CanConnectToTargetAsync(connectionString, logger))
        {
            logger.LogInformation("Banco de dados '{Database}' acessível. Pulando provisionamento.", targetDatabase);
            return;
        }

        // Substitui o banco alvo pelo banco de manutenção 'postgres' (cenário VPS: banco novo)
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

    /// <summary>
    /// Tenta conectar diretamente ao banco alvo. Retorna true se o banco existe e aceita
    /// conexão. Faz retry curto para tolerar o "wake" de bancos serverless (ex.: Neon
    /// scale-to-zero, que acorda em ~0,5-2s). Retorna false se o banco não existe
    /// (ex.: VPS com Postgres recém-provisionado), caso em que o provisionamento prossegue.
    /// </summary>
    private static async Task<bool> CanConnectToTargetAsync(string connectionString, ILogger logger)
    {
        const int tentativas = 5;
        for (var i = 1; i <= tentativas; i++)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                return true;
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == PostgresErrorCodes.InvalidCatalogName)
            {
                // "database does not exist" — precisa provisionar. Não adianta repetir.
                return false;
            }
            catch (Exception ex) when (i < tentativas)
            {
                // Pode ser cold start do banco serverless ou indisponibilidade momentânea.
                logger.LogInformation(
                    "Aguardando o banco alvo ficar disponível (tentativa {Tentativa}/{Max}): {Mensagem}",
                    i, tentativas, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        // Não conseguimos confirmar conexão ao alvo; deixa o fluxo de manutenção decidir.
        return false;
    }
}
