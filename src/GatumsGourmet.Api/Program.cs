using System.Text;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Api.Middleware;
using GatumsGourmet.Application;
using GatumsGourmet.Infrastructure;
using GatumsGourmet.Infrastructure.Data;
using GatumsGourmet.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("AdminOnly", p => p.RequireRole("admin"));
    opt.AddPolicy("ClienteOnly", p => p.RequireRole("cliente"));
});

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// OpenAPI nativo do .NET 10 com segurança JWT configurada para o Scalar
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Gatums Gourmet API",
            Version = "v1",
            Description = "API do sistema Gatums Gourmet"
        };

        // Esquema de segurança JWT Bearer para o Scalar
        /*document.Components ??= new();
        document.Components.SecuritySchemes = new Dictionary<string, Microsoft.OpenApi.Models.OpenApiSecurityScheme>
        {
            ["Bearer"] = new()
            {
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Informe o token JWT no campo abaixo. Exemplo: eyJhbGci..."
            }
        };*/

        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("LocalDocker"))
{
    // Gera o endpoint JSON do OpenAPI (ex: /openapi/v1.json)
    app.MapOpenApi();
    
    // Ativa a interface visual do Scalar apontando para o endpoint nativo
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Gatums Gourmet Documentation")
               .WithTheme(ScalarTheme.DeepSpace)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.MapGatumsEndpoints();

// Garante que o banco de dados existe e aplica as migrations antes de iniciar a API.
// Resolve o erro "database does not exist" em ambientes locais (Docker) e em Produção
// onde o banco ainda não foi provisionado. Conecta ao banco de manutenção 'postgres'
// para criar o banco alvo se necessário, depois aplica todas as migrations pendentes.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GatumsDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await EnsureDatabaseExistsAsync(db, logger);

    logger.LogInformation("Aplicando migrations...");
    await db.Database.MigrateAsync();
    logger.LogInformation("Migrations aplicadas com sucesso.");

    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DbSeeder.SeedAsync(db, hasher);
}

app.Run();

/// <summary>
/// Conecta ao banco de manutenção 'postgres' e cria o banco alvo caso não exista.
/// Esta abordagem é necessária porque o EF Core não consegue criar o banco automaticamente
/// com MigrateAsync quando o banco de dados não existe — o PostgreSQL exige uma conexão
/// a um banco existente para poder criar outro.
/// </summary>
static async Task EnsureDatabaseExistsAsync(GatumsDbContext db, ILogger logger)
{
    var connectionString = db.Database.GetConnectionString()
        ?? throw new InvalidOperationException("Connection string não configurada.");

    var csBuilder = new NpgsqlConnectionStringBuilder(connectionString);
    var targetDatabase = csBuilder.Database
        ?? throw new InvalidOperationException("Nome do banco não encontrado na connection string.");

    // Substitui o banco alvo pelo banco de manutenção 'postgres'
    csBuilder.Database = "postgres";
    var maintenanceCs = csBuilder.ToString();

    try
    {
        await using var conn = new NpgsqlConnection(maintenanceCs);
        await conn.OpenAsync();

        await using var checkCmd = new NpgsqlCommand(
            $"SELECT 1 FROM pg_database WHERE datname = '{targetDatabase}'", conn);
        var exists = await checkCmd.ExecuteScalarAsync();

        if (exists is null)
        {
            logger.LogInformation("Banco de dados '{Database}' não encontrado. Criando...", targetDatabase);
            await using var createCmd = new NpgsqlCommand(
                $"CREATE DATABASE \"{targetDatabase}\"", conn);
            await createCmd.ExecuteNonQueryAsync();
            logger.LogInformation("Banco de dados '{Database}' criado com sucesso.", targetDatabase);
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex,
            "Não foi possível verificar/criar o banco '{Database}' via manutenção. " +
            "Assumindo que já existe e prosseguindo com MigrateAsync.", targetDatabase);
    }
}
