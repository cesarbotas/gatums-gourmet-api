using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Usuarios.DeleteUsuario;
using GatumsGourmet.Application.UseCases.V1.Usuarios.SetUsuarioAtivo;
using GatumsGourmet.Application.UseCases.V1.Usuarios.SetUsuarioRoles;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Repositories;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Usuarios;

/// <summary>
/// Cobertura DETERMINÍSTICA da regra crítica "nunca deixar o sistema sem admin
/// ativo". Os testes mockam <see cref="IUsuarioRepository.CountAdminsAtivosAsync"/>
/// retornando 1 para simular que o alvo é o único admin ativo — isso torna o
/// comportamento independente do estado do banco (onde factories de teste podem
/// criar vários admins). Afeta SetUsuarioRoles (despromover), SetUsuarioAtivo
/// (desativar) e DeleteUsuario (soft-delete).
/// </summary>
public class UltimoAdminRuleTests
{
    private const int AdminRoleId = 1;
    private const int GerenteRoleId = 2;

    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IRoleRepository> _roles = new();

    private static Usuario AdminAtivo(Guid id) => new()
    {
        Id = id,
        Nome = "Admin",
        Email = "admin@gatums.com",
        Ativo = true,
        UsuarioRoles = new List<UsuarioRole>
        {
            new() { RoleId = AdminRoleId, Role = new Role { Id = AdminRoleId, Nome = "Admin" } }
        }
    };

    // ---- SetUsuarioRoles ----

    [Fact]
    public async Task SetRoles_RemoveAdminDoUltimoAdminAtivo_Bloqueia()
    {
        var id = Guid.NewGuid();
        _usuarios.Setup(r => r.GetByIdComRolesAsync(id, default)).ReturnsAsync(AdminAtivo(id));
        _roles.Setup(r => r.GetByIdAsync(GerenteRoleId, default))
            .ReturnsAsync(new Role { Id = GerenteRoleId, Nome = "Gerente" });
        _roles.Setup(r => r.GetByNomeAsync("Admin", default))
            .ReturnsAsync(new Role { Id = AdminRoleId, Nome = "Admin" });
        _usuarios.Setup(r => r.CountAdminsAtivosAsync(default)).ReturnsAsync(1);

        var handler = new SetUsuarioRolesHandler(_usuarios.Object, _roles.Object);
        var result = await handler.Handle(new SetUsuarioRolesRequest(id, new[] { GerenteRoleId }));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("O sistema deve ter ao menos um administrador ativo.");
        _usuarios.Verify(r => r.SetRolesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<int>>(), default), Times.Never);
    }

    [Fact]
    public async Task SetRoles_MantemRoleAdmin_Permite()
    {
        var id = Guid.NewGuid();
        _usuarios.Setup(r => r.GetByIdComRolesAsync(id, default)).ReturnsAsync(AdminAtivo(id));
        _roles.Setup(r => r.GetByIdAsync(AdminRoleId, default))
            .ReturnsAsync(new Role { Id = AdminRoleId, Nome = "Admin" });
        _roles.Setup(r => r.GetByIdAsync(GerenteRoleId, default))
            .ReturnsAsync(new Role { Id = GerenteRoleId, Nome = "Gerente" });
        _roles.Setup(r => r.GetByNomeAsync("Admin", default))
            .ReturnsAsync(new Role { Id = AdminRoleId, Nome = "Admin" });
        _usuarios.Setup(r => r.CountAdminsAtivosAsync(default)).ReturnsAsync(1);

        var handler = new SetUsuarioRolesHandler(_usuarios.Object, _roles.Object);
        var result = await handler.Handle(new SetUsuarioRolesRequest(id, new[] { AdminRoleId, GerenteRoleId }));

        result.IsSuccess.Should().BeTrue();
        _usuarios.Verify(r => r.SetRolesAsync(id, It.IsAny<IEnumerable<int>>(), default), Times.Once);
    }

    [Fact]
    public async Task SetRoles_DespromoveMasHaOutroAdminAtivo_Permite()
    {
        var id = Guid.NewGuid();
        _usuarios.Setup(r => r.GetByIdComRolesAsync(id, default)).ReturnsAsync(AdminAtivo(id));
        _roles.Setup(r => r.GetByIdAsync(GerenteRoleId, default))
            .ReturnsAsync(new Role { Id = GerenteRoleId, Nome = "Gerente" });
        _roles.Setup(r => r.GetByNomeAsync("Admin", default))
            .ReturnsAsync(new Role { Id = AdminRoleId, Nome = "Admin" });
        _usuarios.Setup(r => r.CountAdminsAtivosAsync(default)).ReturnsAsync(2);

        var handler = new SetUsuarioRolesHandler(_usuarios.Object, _roles.Object);
        var result = await handler.Handle(new SetUsuarioRolesRequest(id, new[] { GerenteRoleId }));

        result.IsSuccess.Should().BeTrue();
        _usuarios.Verify(r => r.SetRolesAsync(id, It.IsAny<IEnumerable<int>>(), default), Times.Once);
    }

    // ---- SetUsuarioAtivo ----

    [Fact]
    public async Task SetAtivo_DesativaUltimoAdminAtivo_Bloqueia()
    {
        var id = Guid.NewGuid();
        _usuarios.Setup(r => r.GetByIdComRolesAsync(id, default)).ReturnsAsync(AdminAtivo(id));
        _usuarios.Setup(r => r.CountAdminsAtivosAsync(default)).ReturnsAsync(1);

        var handler = new SetUsuarioAtivoHandler(_usuarios.Object);
        var result = await handler.Handle(new SetUsuarioAtivoRequest(id, false));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("O sistema deve ter ao menos um administrador ativo.");
        _usuarios.Verify(r => r.SetAtivoAsync(It.IsAny<Guid>(), It.IsAny<bool>(), default), Times.Never);
    }

    [Fact]
    public async Task SetAtivo_DesativaMasHaOutroAdminAtivo_Permite()
    {
        var id = Guid.NewGuid();
        _usuarios.Setup(r => r.GetByIdComRolesAsync(id, default)).ReturnsAsync(AdminAtivo(id));
        _usuarios.Setup(r => r.CountAdminsAtivosAsync(default)).ReturnsAsync(2);

        var handler = new SetUsuarioAtivoHandler(_usuarios.Object);
        var result = await handler.Handle(new SetUsuarioAtivoRequest(id, false));

        result.IsSuccess.Should().BeTrue();
        _usuarios.Verify(r => r.SetAtivoAsync(id, false, default), Times.Once);
    }

    [Fact]
    public async Task SetAtivo_AtivarUltimoAdmin_NaoAplicaRegra()
    {
        var id = Guid.NewGuid();
        _usuarios.Setup(r => r.GetByIdComRolesAsync(id, default)).ReturnsAsync(AdminAtivo(id));
        _usuarios.Setup(r => r.CountAdminsAtivosAsync(default)).ReturnsAsync(1);

        var handler = new SetUsuarioAtivoHandler(_usuarios.Object);
        var result = await handler.Handle(new SetUsuarioAtivoRequest(id, true));

        result.IsSuccess.Should().BeTrue();
        _usuarios.Verify(r => r.SetAtivoAsync(id, true, default), Times.Once);
        _usuarios.Verify(r => r.CountAdminsAtivosAsync(default), Times.Never);
    }

    // ---- DeleteUsuario (soft-delete) ----

    [Fact]
    public async Task Delete_UltimoAdminAtivo_Bloqueia()
    {
        var id = Guid.NewGuid();
        _usuarios.Setup(r => r.GetByIdComRolesAsync(id, default)).ReturnsAsync(AdminAtivo(id));
        _usuarios.Setup(r => r.CountAdminsAtivosAsync(default)).ReturnsAsync(1);

        var handler = new DeleteUsuarioHandler(_usuarios.Object);
        var result = await handler.Handle(id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("O sistema deve ter ao menos um administrador ativo.");
        _usuarios.Verify(r => r.SetAtivoAsync(It.IsAny<Guid>(), It.IsAny<bool>(), default), Times.Never);
    }

    [Fact]
    public async Task Delete_AdminMasHaOutroAtivo_FazSoftDelete()
    {
        var id = Guid.NewGuid();
        _usuarios.Setup(r => r.GetByIdComRolesAsync(id, default)).ReturnsAsync(AdminAtivo(id));
        _usuarios.Setup(r => r.CountAdminsAtivosAsync(default)).ReturnsAsync(2);

        var handler = new DeleteUsuarioHandler(_usuarios.Object);
        var result = await handler.Handle(id);

        result.IsSuccess.Should().BeTrue();
        // Soft-delete: apenas desativa, nunca remove a linha.
        _usuarios.Verify(r => r.SetAtivoAsync(id, false, default), Times.Once);
    }

    [Fact]
    public async Task Delete_UsuarioNaoAdmin_FazSoftDeleteSemChecarRegra()
    {
        var id = Guid.NewGuid();
        var gerente = new Usuario
        {
            Id = id,
            Nome = "Gerente",
            Ativo = true,
            UsuarioRoles = new List<UsuarioRole>
            {
                new() { RoleId = GerenteRoleId, Role = new Role { Id = GerenteRoleId, Nome = "Gerente" } }
            }
        };
        _usuarios.Setup(r => r.GetByIdComRolesAsync(id, default)).ReturnsAsync(gerente);

        var handler = new DeleteUsuarioHandler(_usuarios.Object);
        var result = await handler.Handle(id);

        result.IsSuccess.Should().BeTrue();
        _usuarios.Verify(r => r.SetAtivoAsync(id, false, default), Times.Once);
        _usuarios.Verify(r => r.CountAdminsAtivosAsync(default), Times.Never);
    }

    [Fact]
    public async Task Delete_UsuarioInexistente_RetornaErro()
    {
        var id = Guid.NewGuid();
        _usuarios.Setup(r => r.GetByIdComRolesAsync(id, default)).ReturnsAsync((Usuario?)null);

        var handler = new DeleteUsuarioHandler(_usuarios.Object);
        var result = await handler.Handle(id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Usuário não encontrado.");
    }
}
