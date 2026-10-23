namespace GatumsGourmet.Domain.Services.Interfaces;

public interface IJwtTokenGenerator
{
    string Generate(Guid userId, string email, string role);
}
