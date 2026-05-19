namespace GatumsGourmet.Application.UseCases.Auth.GetCurrentUser;

public record GetCurrentUserResponse(Guid Id, string Nome, string Email, string Role);
