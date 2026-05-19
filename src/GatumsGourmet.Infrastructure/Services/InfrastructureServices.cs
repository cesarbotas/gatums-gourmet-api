using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GatumsGourmet.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}

public class JwtTokenGenerator(IConfiguration config) : IJwtTokenGenerator
{
    public string Generate(Guid userId, string email, string role)
    {
        var secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// Mock implementations for Development — replace with Stripe/PagSeguro in Production
public class MockPaymentGateway : IPaymentGateway
{
    public Task<CartaoResult> ProcessarAsync(Guid transacaoId, decimal valor, MetodoPagamento metodo, CartaoInput cartao, CancellationToken ct = default)
    {
        var ultimos4 = cartao.Numero.Length >= 4 ? cartao.Numero[^4..] : "0000";
        var result = new CartaoResult(true, $"mock-ref-{transacaoId}", ultimos4, "VISA", null);
        return Task.FromResult(result);
    }
}

public class MockPixProvider : IPixProvider
{
    public Task<PixResult> GerarAsync(Guid transacaoId, decimal valor, CancellationToken ct = default)
    {
        var copiaCola = $"00020126580014BR.GOV.BCB.PIX0136{transacaoId}5204000053039865802BR5913GatumsGourmet6009SAO PAULO62070503***6304ABCD";
        return Task.FromResult(new PixResult(copiaCola, DateTime.UtcNow.AddMinutes(30)));
    }

    public Task<bool> ConfirmarAsync(Guid transacaoId, CancellationToken ct = default) =>
        Task.FromResult(true);
}
