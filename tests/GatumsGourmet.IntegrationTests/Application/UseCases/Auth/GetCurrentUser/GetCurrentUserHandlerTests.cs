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

[Collection("Database Collection")]
public class GetCurrentUserHandlerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GetCurrentUserHandlerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Handle_Deve_Retornar_Usuario_Quando_Existir_No_Banco()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GatumsDbContext>();
        
        // Garante que o banco está criado/migrado
        await dbContext.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var usuario = new Usuario
        {
            Id = userId,
            Nome = "César Botas",
            Email = "cesar@gatums.com",
            SenhaHash = "hash_fake_para_teste",
            Role = UserRole.Admin, // Ajuste conforme os valores do seu Enum
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
        Assert.Equal("cesar@gatums.com", result.Data.Email);
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