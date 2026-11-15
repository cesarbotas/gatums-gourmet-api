using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatumsGourmet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendaPresencialCanal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Troco",
                table: "TransacoesPagamento",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorRecebido",
                table: "TransacoesPagamento",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Canal",
                table: "Pedidos",
                type: "text",
                nullable: false,
                defaultValue: "");

            // ── Seed: permissão vendas_presenciais:criar ───────
            migrationBuilder.Sql(@"
                INSERT INTO ""Permissions"" (""Codigo"", ""Recurso"", ""Acao"", ""Descricao"")
                VALUES ('vendas_presenciais:criar', 'vendas_presenciais', 'criar', 'Criar venda presencial')
                ON CONFLICT (""Codigo"") DO NOTHING;

                INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"")
                SELECT r.""Id"", p.""Id""
                FROM ""Roles"" r CROSS JOIN ""Permissions"" p
                WHERE r.""Nome"" = 'Admin' AND p.""Codigo"" = 'vendas_presenciais:criar'
                ON CONFLICT DO NOTHING;

                INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"")
                SELECT r.""Id"", p.""Id""
                FROM ""Roles"" r JOIN ""Permissions"" p ON p.""Codigo"" = 'vendas_presenciais:criar'
                WHERE r.""Nome"" = 'Gerente'
                ON CONFLICT DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Troco",
                table: "TransacoesPagamento");

            migrationBuilder.DropColumn(
                name: "ValorRecebido",
                table: "TransacoesPagamento");

            migrationBuilder.DropColumn(
                name: "Canal",
                table: "Pedidos");
        }
    }
}
