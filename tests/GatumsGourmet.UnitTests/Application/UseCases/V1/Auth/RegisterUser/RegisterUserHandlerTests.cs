using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Auth.RegisterUser;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Services.Interfaces;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Auth.RegisterUser;

public class RegisterUserHandlerTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IClock> _clock = new();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 1, 1));
        _handler = new RegisterUserHandler(_usuarios.Object, _hasher.Object, _clock.Object);
    }

    [Fact]
    public async Task Handle_DadosValidos_CriaUsuarioERetornaId()
    {
        _usuarios.Setup(r => r.GetByEmailAsync("novo@x.com", default)).ReturnsAsync((Usuario?)null);
        _hasher.Setup(h => h.Hash("Senha@123")).Returns("hashed");
        _usuarios.Setup(r => r.AddAsync(It.IsAny<Usuario>(), default)).Returns(Task.CompletedTask);
        _usuarios.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new RegisterUserRequest("Novo", "novo@x.com", "Senha@123", "119"));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().NotBeEmpty();
        result.Data.Email.Should().Be("novo@x.com");
        result.Data.Role.Should().Be("cliente");
        _usuarios.Verify(r => r.AddAsync(It.Is<Usuario>(u => u.SenhaHash == "hashed" && u.Nome == "Novo"), default), Times.Once);
        _usuarios.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailJaCadastrado_RetornaErro()
    {
        var existing = new Usuario { Id = Guid.NewGuid(), Email = "dup@x.com" };
        _usuarios.Setup(r => r.GetByEmailAsync("dup@x.com", default)).ReturnsAsync(existing);

        var result = await _handler.Handle(new RegisterUserRequest("Outro", "dup@x.com", "Senha@123", null));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("já cadastrado");
        _usuarios.Verify(r => r.AddAsync(It.IsAny<Usuario>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_TelefoneOpcional_AceitaNull()
    {
        _usuarios.Setup(r => r.GetByEmailAsync("a@x.com", default)).ReturnsAsync((Usuario?)null);
        _hasher.Setup(h => h.Hash("s")).Returns("h");
        _usuarios.Setup(r => r.AddAsync(It.IsAny<Usuario>(), default)).Returns(Task.CompletedTask);
        _usuarios.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new RegisterUserRequest("A", "a@x.com", "s", null));

        result.IsSuccess.Should().BeTrue();
        _usuarios.Verify(r => r.AddAsync(It.Is<Usuario>(u => u.Telefone == null), default), Times.Once);
    }
}

