using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Usuarios.CreateUsuario;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Usuarios;

public class CreateUsuarioHandlerTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IClock> _clock = new();
    private readonly CreateUsuarioHandler _handler;

    public CreateUsuarioHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 1, 1));
        _handler = new CreateUsuarioHandler(_usuarios.Object, _roles.Object, _hasher.Object, _clock.Object);
    }

    [Fact]
    public async Task Handle_DadosValidos_CriaUsuarioHasheiaSenhaEVinculaRoles()
    {
        _usuarios.Setup(r => r.GetByEmailAsync("novo@x.com", default)).ReturnsAsync((Usuario?)null);
        _roles.Setup(r => r.GetByIdAsync(2, default)).ReturnsAsync(new Role { Id = 2, Nome = "Gerente" });
        _hasher.Setup(h => h.Hash("Senha@123")).Returns("hashed");

        var result = await _handler.Handle(new CreateUsuarioRequest("Novo", "novo@x.com", "Senha@123", "119", new[] { 2 }));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().NotBeEmpty();
        _usuarios.Verify(r => r.AddAsync(It.Is<Usuario>(u =>
            u.SenhaHash == "hashed" && u.Ativo && u.CriadoEm == new DateTime(2026, 1, 1)), default), Times.Once);
        _usuarios.Verify(r => r.SetRolesAsync(It.IsAny<Guid>(), It.Is<IEnumerable<int>>(ids => ids.Contains(2)), default), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailJaCadastrado_RetornaErro()
    {
        _usuarios.Setup(r => r.GetByEmailAsync("dup@x.com", default))
            .ReturnsAsync(new Usuario { Id = Guid.NewGuid(), Email = "dup@x.com" });

        var result = await _handler.Handle(new CreateUsuarioRequest("X", "dup@x.com", "Senha@123", null, Array.Empty<int>()));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("já cadastrado");
        _usuarios.Verify(r => r.AddAsync(It.IsAny<Usuario>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_RoleInexistente_RetornaErroAntesDePersistir()
    {
        _usuarios.Setup(r => r.GetByEmailAsync("a@x.com", default)).ReturnsAsync((Usuario?)null);
        _roles.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Role?)null);

        var result = await _handler.Handle(new CreateUsuarioRequest("A", "a@x.com", "Senha@123", null, new[] { 99 }));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("não encontrada");
        _usuarios.Verify(r => r.AddAsync(It.IsAny<Usuario>(), default), Times.Never);
    }
}
