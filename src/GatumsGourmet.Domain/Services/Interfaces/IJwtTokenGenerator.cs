namespace GatumsGourmet.Domain.Services.Interfaces;

public interface IJwtTokenGenerator
{
    string Generate(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissoes);
}
