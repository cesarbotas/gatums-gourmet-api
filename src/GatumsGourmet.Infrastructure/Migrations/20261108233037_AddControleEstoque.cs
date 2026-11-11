using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatumsGourmet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddControleEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Estoque",
                table: "Produtos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EstoqueMinimo",
                table: "Produtos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MovimentosEstoque",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    SaldoResultante = table.Column<int>(type: "integer", nullable: false),
                    PedidoId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentosEstoque", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Estoque",
                table: "Produtos",
                column: "Estoque");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_Data",
                table: "MovimentosEstoque",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_ProdutoId",
                table: "MovimentosEstoque",
                column: "ProdutoId");

            // ── Seed: permissão estoque:gerenciar + vínculos ──
            migrationBuilder.Sql(@"
                INSERT INTO ""Permissions"" (""Codigo"", ""Recurso"", ""Acao"", ""Descricao"")
                VALUES ('estoque:gerenciar', 'estoque', 'gerenciar', 'Ajustar estoque')
                ON CONFLICT (""Codigo"") DO NOTHING;

                -- Admin recebe a nova permissão
                INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"")
                SELECT r.""Id"", p.""Id""
                FROM ""Roles"" r CROSS JOIN ""Permissions"" p
                WHERE r.""Nome"" = 'Admin' AND p.""Codigo"" = 'estoque:gerenciar'
                ON CONFLICT DO NOTHING;

                -- Gerente recebe a nova permissão
                INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"")
                SELECT r.""Id"", p.""Id""
                FROM ""Roles"" r JOIN ""Permissions"" p ON p.""Codigo"" = 'estoque:gerenciar'
                WHERE r.""Nome"" = 'Gerente'
                ON CONFLICT DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimentosEstoque");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_Estoque",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Estoque",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "EstoqueMinimo",
                table: "Produtos");
        }
    }
}
