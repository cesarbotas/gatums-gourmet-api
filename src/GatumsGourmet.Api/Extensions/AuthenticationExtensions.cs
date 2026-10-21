using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GatumsGourmet.Api.Extensions;

/// <summary>
/// Configura a autenticação JWT Bearer e as políticas de autorização da API.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddGatumsAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddAuthorization(opt =>
        {
            opt.AddPolicy("AdminOnly", p => p.RequireRole("admin"));
            opt.AddPolicy("ClienteOnly", p => p.RequireRole("cliente"));
        });

        return services;
    }
}
