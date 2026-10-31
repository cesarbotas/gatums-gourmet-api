namespace GatumsGourmet.Application.UseCases.V1.Auth.RegisterUser;

public record RegisterUserResponse(Guid Id, string Nome, string Email, IReadOnlyList<string> Roles);
