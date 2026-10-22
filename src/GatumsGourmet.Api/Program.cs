using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Api.Middleware;
using GatumsGourmet.Application;
using GatumsGourmet.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddGatumsAuthentication(builder.Configuration);
builder.Services.AddGatumsOpenApi();

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);

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

await app.InitializeDatabaseAsync();

app.Run();

// Exposto para os testes de integração (WebApplicationFactory<Program>).
public partial class Program { }
