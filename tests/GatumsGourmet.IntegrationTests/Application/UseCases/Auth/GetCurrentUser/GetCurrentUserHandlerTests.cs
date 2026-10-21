using GatumsGourmet.Application.UseCases.Auth.GetCurrentUser;
using GatumsGourmet.Domain.Entities;
using GatumsGourmet.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using GatumsGourmet.IntegrationTests.Factory;
using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Enums.Auth;

namespace GatumsGourmet.IntegrationTests.Application.UseCases.Auth.GetCurrentUser;

[Collection("Integration")]
public class GetCurrentUserHandlerIntegrationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public GetCurrentUserHandlerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Handle_Deve_Retornar_Usuario_Quando_Existir_No_Banco()
    {
        // Arrange — o banco já foi migrado no startup (InitializeDatabaseAsync); não usar
        // EnsureCreatedAsync, que conflita com o histórico de migrations.
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GatumsDbContext>();

        var userId = Guid.NewGuid();
        // E-mail único por execução: o banco de teste é persistente entre rodadas,
        // então um e-mail fixo causaria violação de unicidade.
        var email = $"cesar-{Guid.NewGuid():N}@gatums.com";
        var usuario = new Usuario
        {
            Id = userId,
            Nome = "César Botas",
            Email = email,
            SenhaHash = "hash_fake_para_teste",
            Role = UserRole.Admin,
            CriadoEm = DateTime.UtcNow
        };

        dbContext.Usuarios.Add(usuario);
        await dbContext.SaveChangesAsync();

        var handler = scope.ServiceProvider.GetRequiredService<IGetCurrentUserHandler>();

        // Act
        var result = await handler.Handle(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(userId, result.Data.Id);
        Assert.Equal("César Botas", result.Data.Nome);
        Assert.Equal(email, result.Data.Email);
        Assert.Equal("admin", result.Data.Role);
    }

    [Fact]
    public async Task Handle_Deve_Retornar_Falha_Quando_Usuario_Nao_Existir_No_Banco()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IGetCurrentUserHandler>();

        var userIdInexistente = Guid.NewGuid();

        // Act
        var result = await handler.Handle(userIdInexistente);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Usuário não encontrado.", result.Error);
    }
}