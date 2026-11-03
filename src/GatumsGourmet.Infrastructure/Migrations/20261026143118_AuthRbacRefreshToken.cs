using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GatumsGourmet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuthRbacRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IMPORTANTE: a ordem abaixo é crítica.
            // 1) Cria as 5 tabelas de RBAC/refresh e a coluna Ativo.
            // 2) Semeia permissões, roles e vínculos (idempotente, ON CONFLICT DO NOTHING).
            // 3) Migra o enum legado "Role" (text) de Usuarios para UsuarioRoles — SÓ se a
            //    coluna Role ainda existir no banco (bancos antigos). Protegido por checagem.
            // 4) Só então remove a coluna legada "Role".
            // O DropColumn("Role") gerado originalmente pelo EF no início foi MOVIDO para o fim.

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Recurso = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Acao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FamiliaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Revogado = table.Column<bool>(type: "boolean", nullable: false),
                    RevogadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubstituidoPorTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MotivoRevogacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CriadoPorIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Sistema = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    PermissionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioRoles",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRoles", x => new { x.UsuarioId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Codigo",
                table: "Permissions",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_FamiliaId",
                table: "RefreshTokens",
                column: "FamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UsuarioId",
                table: "RefreshTokens",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Nome",
                table: "Roles",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_RoleId",
                table: "UsuarioRoles",
                column: "RoleId");

            // ----------------------------------------------------------------------------
            // 2) SEED idempotente de permissões, roles e vínculos.
            // ----------------------------------------------------------------------------

            // 12 permissões no formato recurso:acao.
            migrationBuilder.Sql(@"
INSERT INTO ""Permissions"" (""Codigo"", ""Recurso"", ""Acao"", ""Descricao"") VALUES
    ('produtos:ler',        'produtos',   'ler',      'Visualizar produtos'),
    ('produtos:escrever',   'produtos',   'escrever', 'Criar e editar produtos'),
    ('categorias:escrever', 'categorias', 'escrever', 'Criar e editar categorias'),
    ('clientes:ler',        'clientes',   'ler',      'Visualizar clientes'),
    ('clientes:escrever',   'clientes',   'escrever', 'Criar e editar clientes'),
    ('pedidos:ler',         'pedidos',    'ler',      'Visualizar pedidos'),
    ('pedidos:avancar',     'pedidos',    'avancar',  'Avancar o status de pedidos'),
    ('pagamentos:validar',  'pagamentos', 'validar',  'Validar pagamentos'),
    ('relatorios:ler',      'relatorios', 'ler',      'Visualizar relatorios'),
    ('usuarios:gerenciar',  'usuarios',   'gerenciar','Gerenciar usuarios'),
    ('roles:gerenciar',     'roles',      'gerenciar','Gerenciar roles'),
    ('permissoes:gerenciar','permissoes', 'gerenciar','Gerenciar permissoes')
ON CONFLICT (""Codigo"") DO NOTHING;
");

            // 3 roles de sistema.
            migrationBuilder.Sql(@"
INSERT INTO ""Roles"" (""Nome"", ""Descricao"", ""Sistema"") VALUES
    ('Admin',   'Acesso total ao sistema', true),
    ('Cliente', 'Cliente final sem acesso administrativo', true),
    ('Gerente', 'Operacao de loja com acesso parcial', false)
ON CONFLICT (""Nome"") DO NOTHING;
");

            // Admin recebe TODAS as 12 permissões.
            migrationBuilder.Sql(@"
INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"")
SELECT r.""Id"", p.""Id""
FROM ""Roles"" r
CROSS JOIN ""Permissions"" p
WHERE r.""Nome"" = 'Admin'
ON CONFLICT DO NOTHING;
");

            // Gerente recebe 8 permissões específicas.
            migrationBuilder.Sql(@"
INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"")
SELECT r.""Id"", p.""Id""
FROM ""Roles"" r
JOIN ""Permissions"" p ON p.""Codigo"" IN (
    'produtos:escrever',
    'categorias:escrever',
    'clientes:ler',
    'clientes:escrever',
    'pedidos:ler',
    'pedidos:avancar',
    'relatorios:ler',
    'pagamentos:validar'
)
WHERE r.""Nome"" = 'Gerente'
ON CONFLICT DO NOTHING;
");

            // Cliente NÃO recebe nenhuma permissão (nenhum insert).

            // ----------------------------------------------------------------------------
            // 3) DATA MIGRATION do enum legado. Só roda se a coluna legada "Role" existir
            //    (bancos criados do zero após a Task 1 nunca a tiveram). A coluna Role foi
            //    HasConversion<string>, então contém o texto 'Admin'/'Cliente'.
            //    Executada ANTES do DropColumn("Role").
            // ----------------------------------------------------------------------------
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'Usuarios' AND column_name = 'Role'
    ) THEN
        -- Garante Ativo = true para linhas pré-existentes (o default só vale para novas linhas).
        UPDATE ""Usuarios"" SET ""Ativo"" = true WHERE ""Ativo"" IS DISTINCT FROM true;

        -- Vincula cada usuário legado à role cujo Nome bate com o texto da coluna Role.
        INSERT INTO ""UsuarioRoles"" (""UsuarioId"", ""RoleId"")
        SELECT u.""Id"", r.""Id""
        FROM ""Usuarios"" u
        JOIN ""Roles"" r ON r.""Nome"" = u.""Role""
        ON CONFLICT DO NOTHING;
    END IF;
END $$;
");

            // ----------------------------------------------------------------------------
            // 4) Remove a coluna legada POR ÚLTIMO. DropColumnOperation com IfExists via SQL
            //    para ser seguro em bancos que já não tinham a coluna.
            // ----------------------------------------------------------------------------
            migrationBuilder.Sql(@"ALTER TABLE ""Usuarios"" DROP COLUMN IF EXISTS ""Role"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recria a coluna legada "Role" (text, nullable) e copia de volta a role de MAIOR
            // privilégio de cada usuário (Admin > Gerente > Cliente).
            // ATENÇÃO: roles múltiplas por usuário se PERDEM no rollback — o modelo legado só
            // suportava uma role por usuário.
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Usuarios",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE ""Usuarios"" u
SET ""Role"" = sub.""Nome""
FROM (
    SELECT ur.""UsuarioId"",
           (ARRAY_AGG(r.""Nome"" ORDER BY
               CASE r.""Nome""
                   WHEN 'Admin'   THEN 1
                   WHEN 'Gerente' THEN 2
                   WHEN 'Cliente' THEN 3
                   ELSE 4
               END))[1] AS ""Nome""
    FROM ""UsuarioRoles"" ur
    JOIN ""Roles"" r ON r.""Id"" = ur.""RoleId""
    GROUP BY ur.""UsuarioId""
) sub
WHERE sub.""UsuarioId"" = u.""Id"";
");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UsuarioRoles");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Usuarios");
        }
    }
}
