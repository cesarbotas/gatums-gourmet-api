using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Enums.Produtos;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(GatumsDbContext db, IPasswordHasher hasher)
    {
        await db.Database.MigrateAsync();

        if (!await db.Usuarios.AnyAsync())
        {
            var admin = new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = "Admin",
                Email = "admin@gatums.com",
                SenhaHash = hasher.Hash("Admin@123"),
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            };
            db.Usuarios.Add(admin);
            await db.SaveChangesAsync();

            // Vincula o Admin inicial à role Admin (semeada pela migration, que roda
            // antes deste ponto via MigrateAsync acima).
            var roleAdmin = await db.Roles.FirstOrDefaultAsync(r => r.Nome == "Admin");
            if (roleAdmin is not null &&
                !await db.UsuarioRoles.AnyAsync(ur => ur.UsuarioId == admin.Id && ur.RoleId == roleAdmin.Id))
            {
                db.UsuarioRoles.Add(new UsuarioRole { UsuarioId = admin.Id, RoleId = roleAdmin.Id });
                await db.SaveChangesAsync();
            }
        }

        if (!await db.Categorias.AnyAsync())
        {
            db.Categorias.AddRange(
                new Categoria { Nome = "Hambúrgueres", Descricao = "Lanches artesanais", Ativo = true },
                new Categoria { Nome = "Acompanhamentos", Descricao = "Porções e extras", Ativo = true },
                new Categoria { Nome = "Bebidas", Descricao = "Refrigerantes, sucos e água", Ativo = true }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Produtos.AnyAsync())
        {
            var hamburgueres = await db.Categorias.FirstAsync(c => c.Nome == "Hambúrgueres");
            var acompanhamentos = await db.Categorias.FirstAsync(c => c.Nome == "Acompanhamentos");
            var bebidas = await db.Categorias.FirstAsync(c => c.Nome == "Bebidas");

            db.Produtos.AddRange(
                new Produto { Id = Guid.NewGuid(), Nome = "Hambúrguer Artesanal", Descricao = "Blend 180g, queijo cheddar, alface, tomate", CategoriaId = hamburgueres.Id, Preco = 32.90m, FotoUrl = "https://placehold.co/400x300?text=Hamburguer", Disponivel = true, Tipo = TipoProduto.Simples, CriadoEm = DateTime.UtcNow },
                new Produto { Id = Guid.NewGuid(), Nome = "Batata Frita", Descricao = "Porção 300g crocante", CategoriaId = acompanhamentos.Id, Preco = 18.90m, FotoUrl = "https://placehold.co/400x300?text=Batata", Disponivel = true, Tipo = TipoProduto.Simples, CriadoEm = DateTime.UtcNow },
                new Produto { Id = Guid.NewGuid(), Nome = "Refrigerante 350ml", Descricao = "Lata gelada", CategoriaId = bebidas.Id, Preco = 7.90m, FotoUrl = "https://placehold.co/400x300?text=Refri", Disponivel = true, Tipo = TipoProduto.Simples, CriadoEm = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        }
    }
}

