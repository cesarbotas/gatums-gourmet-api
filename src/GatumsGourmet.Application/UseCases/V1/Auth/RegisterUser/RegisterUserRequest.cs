namespace GatumsGourmet.Application.UseCases.V1.Auth.RegisterUser;

public record RegisterUserRequest(string Nome, string Email, string Senha, string? Telefone);
