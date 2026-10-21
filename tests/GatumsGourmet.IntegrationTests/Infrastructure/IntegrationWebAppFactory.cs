using System.Collections.Concurrent;
using GatumsGourmet.Domain.Interfaces;
using GatumsGourmet.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GatumsGourmet.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory para a API Gatums Gourmet.
/// Sobrescreve a connection string para apontar ao PostgreSQL de teste (Docker).
/// </summary>
public class IntegrationWebAppFactory : WebApplicationFactory<Program>
{
    private const string TestJwtSecret = "test-secret-min-32-chars-for-hs256!!";

    private readonly string _connectionString;

    public IntegrationWebAppFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = "GatumsGourmet",
                ["Jwt:Audience"] = "GatumsGourmet.Web"
            });
        });
    }

    /// <summary>
    /// Reseta o banco: trunca todas as tabelas e re-executa o seed.
    /// Chame antes de testes que precisam de estado limpo.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GatumsDbContext>();

        var tableNames = db.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .ToList();

        // Trunca todas as tabelas (CASCADE resolve FKs)
        foreach (var table in tableNames)
        {
            await db.Database.ExecuteSqlRawAsync(
                $"TRUNCATE TABLE \"{table}\" RESTART IDENTITY CASCADE");
        }

        // Re-seed (admin + produtos demo)
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await DbSeeder.SeedAsync(db, hasher);
    }

    /// <summary>
    /// Resolve um handler do container DI (para testes diretos de handler).
    /// </summary>
    public T GetHandler<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }
}
