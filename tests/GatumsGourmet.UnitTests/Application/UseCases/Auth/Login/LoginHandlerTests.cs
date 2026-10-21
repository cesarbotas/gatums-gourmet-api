using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Auth.Login;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Enums.Auth;
using GatumsGourmet.Domain.Interfaces;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Auth.Login;

public class LoginHandlerTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _handler = new LoginHandler(_usuarios.Object, _hasher.Object, _jwt.Object);
    }

    private static Usuario CreateUsuario(string email = "admin@gatums.com", UserRole role = UserRole.Admin)
        => new() { Id = Guid.NewGuid(), Nome = "Admin", Email = email, SenhaHash = "hash", Role = role };

    [Fact]
    public async Task Handle_CredenciaisValidas_RetornaTokenEUsuario()
    {
        var usuario = CreateUsuario();
        _usuarios.Setup(r => r.GetByEmailAsync(usuario.Email, default)).ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verify("Admin@123", "hash")).Returns(true);
        _jwt.Setup(j => j.Generate(usuario.Id, usuario.Email, "admin")).Returns("jwt-token");

        var result = await _handler.Handle(new LoginRequest(usuario.Email, "Admin@123"));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Token.Should().Be("jwt-token");
        result.Data.User.Email.Should().Be(usuario.Email);
        result.Data.User.Role.Should().Be("admin");
    }

    [Fact]
    public async Task Handle_UsuarioInexistente_RetornaErro()
    {
        _usuarios.Setup(r => r.GetByEmailAsync("inexistente@x.com", default)).ReturnsAsync((Usuario?)null);

        var result = await _handler.Handle(new LoginRequest("inexistente@x.com", "qualquer"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Credenciais inválidas.");
    }

    [Fact]
    public async Task Handle_SenhaIncorreta_RetornaErro()
    {
        var usuario = CreateUsuario();
        _usuarios.Setup(r => r.GetByEmailAsync(usuario.Email, default)).ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verify("errada", "hash")).Returns(false);

        var result = await _handler.Handle(new LoginRequest(usuario.Email, "errada"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Credenciais inválidas.");
    }

    [Fact]
    public async Task Handle_RoleCliente_GeraTokenComRoleCliente()
    {
        var usuario = CreateUsuario("cliente@x.com", UserRole.Cliente);
        _usuarios.Setup(r => r.GetByEmailAsync("cliente@x.com", default)).ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verify("senha", "hash")).Returns(true);
        _jwt.Setup(j => j.Generate(usuario.Id, usuario.Email, "cliente")).Returns("token-cliente");

        var result = await _handler.Handle(new LoginRequest("cliente@x.com", "senha"));

        result.IsSuccess.Should().BeTrue();
        result.Data!.User.Role.Should().Be("cliente");
    }
}
