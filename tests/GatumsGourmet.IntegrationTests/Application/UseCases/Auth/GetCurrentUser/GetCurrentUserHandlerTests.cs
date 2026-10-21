using GatumsGourmet.Application.UseCases.Auth.GetCurrentUser;
using GatumsGourmet.Domain.Entities;
using GatumsGourmet.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using GatumsGourmet.IntegrationTests.Factory;
using GatumsGourmet.Application.Common;

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
        // Ajuste a instanciação da entidade Usuario de acordo com o construtor do seu projeto
        var usuario = new Usuario(userId, "César Botas", "cesar@gatums.com", "admin");
        
        dbContext.Usuarios.Add(usuario);
        await dbContext.SaveChangesAsync();

        var handler = scope.ServiceProvider.GetRequiredService<IGetCurrentUserHandler>();

        // Act
        var result = await handler.Handle(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(userId, result.Value.Id);
        Assert.Equal("César Botas", result.Value.Nome);
        Assert.Equal("cesar@gatums.com", result.Value.Email);
        Assert.Equal("admin", result.Value.Role);
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