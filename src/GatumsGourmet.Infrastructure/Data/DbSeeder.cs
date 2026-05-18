using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Enums.Auth;
using GatumsGourmet.Domain.Enums.Produtos;
using GatumsGourmet.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(GatumsDbContext db, IPasswordHasher hasher)
    {
        await db.Database.MigrateAsync();

        if (!await db.Usuarios.AnyAsync())
        {
            db.Usuarios.Add(new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = "Admin",
                Email = "admin@gatums.com",
                SenhaHash = hasher.Hash("Admin@123"),
                Role = UserRole.Admin,
                CriadoEm = DateTime.UtcNow
            });
        }

        if (!await db.Produtos.AnyAsync())
        {
            db.Produtos.AddRange(
                new Produto { Id = Guid.NewGuid(), Nome = "Hambúrguer Artesanal", Descricao = "Blend 180g, queijo cheddar, alface, tomate", Categoria = "Hambúrgueres", Preco = 32.90m, FotoUrl = "https://placehold.co/400x300?text=Hamburguer", Disponivel = true, Tipo = TipoProduto.Simples, CriadoEm = DateTime.UtcNow },
                new Produto { Id = Guid.NewGuid(), Nome = "Batata Frita", Descricao = "Porção 300g crocante", Categoria = "Acompanhamentos", Preco = 18.90m, FotoUrl = "https://placehold.co/400x300?text=Batata", Disponivel = true, Tipo = TipoProduto.Simples, CriadoEm = DateTime.UtcNow },
                new Produto { Id = Guid.NewGuid(), Nome = "Refrigerante 350ml", Descricao = "Lata gelada", Categoria = "Bebidas", Preco = 7.90m, FotoUrl = "https://placehold.co/400x300?text=Refri", Disponivel = true, Tipo = TipoProduto.Simples, CriadoEm = DateTime.UtcNow }
            );
        }

        await db.SaveChangesAsync();
    }
}
