using System;
using System.Text;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Infrastructure.Services.Auth;

public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public (string PlainToken, string Hash) Generate()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var plain = Base64UrlEncode(bytes);
        return (plain, Hash(plain));
    }

    public string Hash(string plainToken)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
