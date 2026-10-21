using GatumsGourmet.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GatumsGourmet.Api;

namespace GatumsGourmet.IntegrationTests.Factory;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove o registro original do DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<GatumsDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Adiciona o DbContext apontando para o PostgreSQL de testes na porta 5433
            services.AddDbContext<GatumsDbContext>(options =>
            {
                options.UseNpgsql("Host=localhost;Port=5433;Database=gatums_test;Username=gatums;Password=gatums_test");
            });

            // Opcional: Aplica as migrações automaticamente no banco de testes ao iniciar
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GatumsDbContext>();
            db.Database.EnsureCreated(); // Ou db.Database.Migrate();
        });
    }
}