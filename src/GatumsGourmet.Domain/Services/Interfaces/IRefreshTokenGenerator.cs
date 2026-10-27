namespace GatumsGourmet.Domain.Services.Interfaces;

public interface IRefreshTokenGenerator
{
    (string PlainToken, string Hash) Generate();
    string Hash(string plainToken);
}
