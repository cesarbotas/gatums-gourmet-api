using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Auth.GetCurrentUser;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Repositories;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Auth.GetCurrentUser;

public class GetCurrentUserHandlerTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly GetCurrentUserHandler _handler;

    public GetCurrentUserHandlerTests() => _handler = new GetCurrentUserHandler(_usuarios.Object);

    private static Usuario CreateUsuarioComRoles(Guid id, string nome, string email, params string[] roles)
    {
        var usuario = new Usuario { Id = id, Nome = nome, Email = email };
        usuario.UsuarioRoles = roles
            .Select(nomeRole => new UsuarioRole
            {
                UsuarioId = id,
                Usuario = usuario,
                Role = new Role { Nome = nomeRole }
            })
            .ToList();
        return usuario;
    }

    [Fact]
    public async Task Handle_UsuarioExistente_RetornaDados()
    {
        var id = Guid.NewGuid();
        var usuario = CreateUsuarioComRoles(id, "Teste", "t@x.com", "Cliente");
        _usuarios.Setup(r => r.GetByIdComRolesAsync(id, default)).ReturnsAsync(usuario);

        var result = await _handler.Handle(id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(id);
        result.Data.Nome.Should().Be("Teste");
        result.Data.Roles.Should().Contain("Cliente");
    }

    [Fact]
    public async Task Handle_UsuarioInexistente_RetornaErro()
    {
        _usuarios.Setup(r => r.GetByIdComRolesAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Usuario?)null);

        var result = await _handler.Handle(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Usuário não encontrado.");
    }

    [Fact]
    public async Task Handle_RoleAdmin_RetornaRoleAdmin()
    {
        var id = Guid.NewGuid();
        var usuario = CreateUsuarioComRoles(id, "Admin", "a@x.com", "Admin");
        _usuarios.Setup(r => r.GetByIdComRolesAsync(id, default)).ReturnsAsync(usuario);

        var result = await _handler.Handle(id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Roles.Should().Contain("Admin");
    }
}
