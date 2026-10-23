namespace GatumsGourmet.Application.UseCases.V1.Auth.Login;

public record LoginUserDto(Guid Id, string Nome, string Email, string Role);
public record LoginResponse(string Token, LoginUserDto User);
