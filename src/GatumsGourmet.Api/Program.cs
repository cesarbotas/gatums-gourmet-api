using GatumsGourmet.Api.BackgroundServices;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Api.Middleware;
using GatumsGourmet.Application;
using GatumsGourmet.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddGatumsAuthentication(builder.Configuration);
builder.Services.AddGatumsOpenApi();

builder.Services.AddHostedService<RefreshTokenCleanupService>();
builder.Services.AddHostedService<OrphanImageCleanupService>();

// CORS por ambiente: Development/LocalDocker liberam tudo; Release/PRD restringem
// às origens/padrões configurados (ver CorsExtensions). Valida no startup.
builder.Services.AddGatumsCors();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseGatumsCors();

app.UseAuthentication();
app.UseAuthorization();

// Scalar/OpenAPI exposto em Development, LocalDocker e Release (homologação) — útil para
// testes externos. Em Production (VPS) fica desabilitado.
if (app.Environment.IsDevelopment()
    || app.Environment.IsEnvironment("LocalDocker")
    || app.Environment.IsEnvironment("Release"))
{
    app.MapGatumsOpenApi();
}

app.MapGatumsEndpoints();

// Diagnóstico de storage no startup (log no host, ex.: Render) — sem expor segredos.
app.LogStorageDiagnostics();

await app.InitializeDatabaseAsync();

app.Run();

// Exposto para os testes de integração (WebApplicationFactory<Program>).
public partial class Program { }
