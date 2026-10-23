using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GatumsGourmet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriaEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Cria a tabela Categorias (Id long identity).
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Nome",
                table: "Categorias",
                column: "Nome",
                unique: true);

            // 2. Popula Categorias com os nomes distintos que já existem em Produtos.Categoria
            //    (preserva as categorias usadas pelos produtos atuais).
            migrationBuilder.Sql(
                @"INSERT INTO ""Categorias"" (""Nome"", ""Descricao"", ""Ativo"")
                  SELECT DISTINCT ""Categoria"", NULL, TRUE
                  FROM ""Produtos""
                  WHERE ""Categoria"" IS NOT NULL AND ""Categoria"" <> ''
                  ON CONFLICT (""Nome"") DO NOTHING;");

            // 3. Adiciona CategoriaId como NULLABLE temporariamente (para preencher).
            migrationBuilder.AddColumn<long>(
                name: "CategoriaId",
                table: "Produtos",
                type: "bigint",
                nullable: true);

            // 4. Preenche CategoriaId pelo match do nome da categoria.
            migrationBuilder.Sql(
                @"UPDATE ""Produtos"" p
                  SET ""CategoriaId"" = c.""Id""
                  FROM ""Categorias"" c
                  WHERE c.""Nome"" = p.""Categoria"";");

            // 5. Para produtos sem categoria correspondente (dados legados), cria/usa
            //    uma categoria 'Sem categoria' e vincula, garantindo NOT NULL.
            migrationBuilder.Sql(
                @"INSERT INTO ""Categorias"" (""Nome"", ""Descricao"", ""Ativo"")
                  SELECT 'Sem categoria', 'Categoria padrão para produtos sem categoria', TRUE
                  WHERE EXISTS (SELECT 1 FROM ""Produtos"" WHERE ""CategoriaId"" IS NULL)
                    AND NOT EXISTS (SELECT 1 FROM ""Categorias"" WHERE ""Nome"" = 'Sem categoria');");

            migrationBuilder.Sql(
                @"UPDATE ""Produtos""
                  SET ""CategoriaId"" = (SELECT ""Id"" FROM ""Categorias"" WHERE ""Nome"" = 'Sem categoria')
                  WHERE ""CategoriaId"" IS NULL;");

            // 6. Torna CategoriaId NOT NULL.
            migrationBuilder.AlterColumn<long>(
                name: "CategoriaId",
                table: "Produtos",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            // 7. Remove a antiga coluna string e cria indice + FK.
            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Produtos");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_CategoriaId",
                table: "Produtos",
                column: "CategoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Produtos_Categorias_CategoriaId",
                table: "Produtos",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Produtos_Categorias_CategoriaId",
                table: "Produtos");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_CategoriaId",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "Produtos");

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Produtos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
