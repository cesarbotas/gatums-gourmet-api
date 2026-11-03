using FluentAssertions;
using GatumsGourmet.Infrastructure.Data;
using GatumsGourmet.IntegrationTests.Factory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.V1.Auth;

/// <summary>
/// Valida que a migration AuthRbacRefreshToken semeou roles, permissões e vínculos
/// corretamente. A migration + o DbSeeder rodam no startup da API (DatabaseExtensions),
/// então estes testes exercitam a migration de verdade contra o Postgres de teste.
///
/// O banco de teste PERSISTE entre execuções — as assertivas verificam ESTADO
/// (contagens e existência), nunca assumem banco vazio, e são idempotentes.
/// </summary>
[Collection("Integration")]
public class MigrationSeedTests
{
    private readonly CustomWebApplicationFactory _factory;

    public MigrationSeedTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        // Garante que o host (e portanto a migration + seed de startup) foi inicializado.
        _factory.CreateClient();
    }

    private GatumsDbContext CriarContexto(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<GatumsDbContext>();

    [Fact]
    public async Task Seed_CriaAsTresRolesDeSistema()
    {
        using var scope = _factory.Services.CreateScope();
        var db = CriarContexto(scope);

        var roles = await db.Roles
            .Where(r => r.Nome == "Admin" || r.Nome == "Cliente" || r.Nome == "Gerente")
            .ToListAsync();

        roles.Select(r => r.Nome).Should().BeEquivalentTo("Admin", "Cliente", "Gerente");
        roles.Single(r => r.Nome == "Admin").Sistema.Should().BeTrue();
        roles.Single(r => r.Nome == "Cliente").Sistema.Should().BeTrue();
        roles.Single(r => r.Nome == "Gerente").Sistema.Should().BeFalse();
    }

    [Fact]
    public async Task Seed_CriaAsDozePermissoes()
    {
        var esperadas = new[]
        {
            "produtos:ler", "produtos:escrever", "categorias:escrever",
            "clientes:ler", "clientes:escrever", "pedidos:ler", "pedidos:avancar",
            "pagamentos:validar", "relatorios:ler", "usuarios:gerenciar",
            "roles:gerenciar", "permissoes:gerenciar"
        };

        using var scope = _factory.Services.CreateScope();
        var db = CriarContexto(scope);

        var codigos = await db.Permissions
            .Where(p => esperadas.Contains(p.Codigo))
            .Select(p => p.Codigo)
            .ToListAsync();

        codigos.Should().BeEquivalentTo(esperadas);
    }

    [Fact]
    public async Task Seed_VinculaPermissoesPorRole()
    {
        using var scope = _factory.Services.CreateScope();
        var db = CriarContexto(scope);

        var admin = await db.Roles.SingleAsync(r => r.Nome == "Admin");
        var cliente = await db.Roles.SingleAsync(r => r.Nome == "Cliente");
        var gerente = await db.Roles.SingleAsync(r => r.Nome == "Gerente");

        var adminCount = await db.RolePermissions.CountAsync(rp => rp.RoleId == admin.Id);
        var clienteCount = await db.RolePermissions.CountAsync(rp => rp.RoleId == cliente.Id);
        var gerenteCount = await db.RolePermissions.CountAsync(rp => rp.RoleId == gerente.Id);

        adminCount.Should().Be(12);
        clienteCount.Should().Be(0);
        gerenteCount.Should().Be(8);
    }

    [Fact]
    public async Task Seed_AdminInicialVinculadoARoleAdmin()
    {
        using var scope = _factory.Services.CreateScope();
        var db = CriarContexto(scope);

        var admin = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == "admin@gatums.com");
        admin.Should().NotBeNull();

        var roleAdmin = await db.Roles.SingleAsync(r => r.Nome == "Admin");

        var vinculado = await db.UsuarioRoles
            .AnyAsync(ur => ur.UsuarioId == admin!.Id && ur.RoleId == roleAdmin.Id);

        vinculado.Should().BeTrue();
    }
}
