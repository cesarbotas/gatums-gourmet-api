using System.Text;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Api.Middleware;
using GatumsGourmet.Application;
using GatumsGourmet.Infrastructure;
using GatumsGourmet.Infrastructure.Data;
using GatumsGourmet.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;// Adicionado para configurações do OpenAPI .NET 10
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

// Configuração OpenAPI nativa do .NET 10 com suporte ao Bearer Token
/* builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Gatums Gourmet API";
        document.Info.Version = "v1";
        
        // Configura a autenticação global JWT para a interface do Scalar
        document.Components ??= new();
        document.Components.SecuritySchemes = requirements;
        
        return Task.CompletedTask;
    });
}); */

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    // Gera o endpoint JSON do OpenAPI (ex: /openapi/v1.json)
    app.MapOpenApi();
    
    // Ativa a interface visual do Scalar apontando para o endpoint nativo
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Gatums Gourmet Documentation")
               .WithTheme(ScalarTheme.DeepSpace) // Escolha seu tema (Default, Purple, DeepSpace, etc.)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.MapGatumsEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GatumsDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DbSeeder.SeedAsync(db, hasher);
}

app.Run();
