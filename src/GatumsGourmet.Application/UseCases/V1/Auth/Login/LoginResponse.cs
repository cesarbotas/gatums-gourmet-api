namespace GatumsGourmet.Application.UseCases.V1.Auth.Login;

public record LoginUserDto(Guid Id, string Nome, string Email, IReadOnlyList<string> Roles);
public record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, LoginUserDto User);
