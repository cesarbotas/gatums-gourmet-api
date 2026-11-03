-- ============================================================================
-- Seed de roles, permissões e vínculos — RBAC Gatums Gourmet
-- ============================================================================
--
-- FONTE DA VERDADE: a migration EF `AuthRbacRefreshToken`
--   (src/GatumsGourmet.Infrastructure/Migrations/..._AuthRbacRefreshToken.cs).
-- A migration já executa este seed automaticamente no startup da API. Este
-- script é um ESPELHO idempotente, para conferência/execução manual (ex.: via
-- DBeaver por túnel SSH — ver docs/Banco/Tunel-SSH-DBeaver.md).
--
-- É seguro rodar múltiplas vezes: todos os INSERTs usam ON CONFLICT DO NOTHING.
-- Nomes de tabela/coluna seguem o padrão do EF/Npgsql: PascalCase com aspas
-- duplas (identificadores case-sensitive no PostgreSQL).
--
-- Conteúdo: 12 permissões, 3 roles de sistema, vínculos role→permissão
--   (Admin = todas as 12, Gerente = 8, Cliente = nenhuma).
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1) Permissões (formato recurso:acao)
-- ----------------------------------------------------------------------------
INSERT INTO "Permissions" ("Codigo", "Recurso", "Acao", "Descricao") VALUES
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
ON CONFLICT ("Codigo") DO NOTHING;

-- ----------------------------------------------------------------------------
-- 2) Roles de sistema
--    Admin e Cliente são roles de sistema (Sistema=true): não podem ser
--    renomeadas, excluídas nem ter permissões alteradas via API.
--    Gerente é uma role comum (Sistema=false).
-- ----------------------------------------------------------------------------
INSERT INTO "Roles" ("Nome", "Descricao", "Sistema") VALUES
    ('Admin',   'Acesso total ao sistema', true),
    ('Cliente', 'Cliente final sem acesso administrativo', true),
    ('Gerente', 'Operacao de loja com acesso parcial', false)
ON CONFLICT ("Nome") DO NOTHING;

-- ----------------------------------------------------------------------------
-- 3) Vínculos role → permissão
-- ----------------------------------------------------------------------------

-- Admin recebe TODAS as 12 permissões (CROSS JOIN com o catálogo).
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
SELECT r."Id", p."Id"
FROM "Roles" r
CROSS JOIN "Permissions" p
WHERE r."Nome" = 'Admin'
ON CONFLICT DO NOTHING;

-- Gerente recebe 8 permissões operacionais.
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
SELECT r."Id", p."Id"
FROM "Roles" r
JOIN "Permissions" p ON p."Codigo" IN (
    'produtos:escrever',
    'categorias:escrever',
    'clientes:ler',
    'clientes:escrever',
    'pedidos:ler',
    'pedidos:avancar',
    'relatorios:ler',
    'pagamentos:validar'
)
WHERE r."Nome" = 'Gerente'
ON CONFLICT DO NOTHING;

-- Cliente NÃO recebe nenhuma permissão (nenhum insert — intencional).

-- ============================================================================
-- Conferência (queries de verificação — opcionais)
-- ============================================================================
-- Total de permissões (esperado: 12):
--   SELECT COUNT(*) FROM "Permissions";
--
-- Permissões por role (esperado: Admin=12, Gerente=8, Cliente=0):
--   SELECT r."Nome", COUNT(rp."PermissionId") AS total
--   FROM "Roles" r
--   LEFT JOIN "RolePermissions" rp ON rp."RoleId" = r."Id"
--   GROUP BY r."Nome"
--   ORDER BY r."Nome";
-- ============================================================================
