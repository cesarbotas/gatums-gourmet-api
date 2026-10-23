using GatumsGourmet.Application.UseCases.V1.Auth.RegisterUser;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Auth.RegisterUser;

/// <summary>
/// Fábrica de requests válidos para o endpoint de registro de usuário.
/// Reutiliza o DTO da própria API (RegisterUserRequest).
/// </summary>
public static class RegisterUserRequestFixture
{
    private static RegisterUserRequest RequestBase(string email) =>
        new(
            Nome: "Usuário de Integração",
            Email: email,
            Senha: "senha123",
            Telefone: "11999998888");

    /// <summary>
    /// Request válido para o caminho feliz. O e-mail é único por chamada para evitar
    /// conflito de cadastro entre execuções no mesmo banco.
    /// </summary>
    public static RegisterUserRequest Sucesso() =>
        RequestBase($"integracao-{Guid.NewGuid():N}@gatums.com");
}
