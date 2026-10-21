using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Auth.GetCurrentUser;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Enums.Auth;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Auth.GetCurrentUser;

public class GetCurrentUserHandlerTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly GetCurrentUserHandler _handler;

    public GetCurrentUserHandlerTests() => _handler = new GetCurrentUserHandler(_usuarios.Object);

    [Fact]
    public async Task Handle_UsuarioExistente_RetornaDados()
    {
        var id = Guid.NewGuid();
        var usuario = new Usuario { Id = id, Nome = "Teste", Email = "t@x.com", Role = UserRole.Cliente };
        _usuarios.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(usuario);

        var result = await _handler.Handle(id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(id);
        result.Data.Nome.Should().Be("Teste");
        result.Data.Role.Should().Be("cliente");
    }

    [Fact]
    public async Task Handle_UsuarioInexistente_RetornaErro()
    {
        _usuarios.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Usuario?)null);

        var result = await _handler.Handle(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Usuário não encontrado.");
    }

    [Fact]
    public async Task Handle_RoleAdmin_RetornaRoleAdmin()
    {
        var id = Guid.NewGuid();
        var usuario = new Usuario { Id = id, Nome = "Admin", Email = "a@x.com", Role = UserRole.Admin };
        _usuarios.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(usuario);

        var result = await _handler.Handle(id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Role.Should().Be("admin");
    }
}
