namespace GatumsGourmet.Application.UseCases.Auth.RegisterUser;

public record RegisterUserRequest(string Nome, string Email, string Senha, string? Telefone);
