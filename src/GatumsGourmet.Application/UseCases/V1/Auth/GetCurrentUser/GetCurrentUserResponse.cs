namespace GatumsGourmet.Application.UseCases.V1.Auth.GetCurrentUser;

public record GetCurrentUserResponse(Guid Id, string Nome, string Email, IReadOnlyList<string> Roles);
