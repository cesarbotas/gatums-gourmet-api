# Task 16: Documentação (ADRs, script de seed, guias, API-Reference)

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. Build 0 erros; 143 unit + 95 integração verdes. CÓDIGO DE AUTH COMPLETO (Tasks 1-15).

**Objetivo:** documentar a feature de autenticação RBAC + refresh token: 4 ADRs, script SQL de seed, guia de permissões, guia de uso, e atualizar o API-Reference.

## Constraints
- Markdown em `docs/`. SQL em `docs/sql/`. noBOM não se aplica a .md (mas evite BOM). Linguagem: português.
- Fonte da verdade: a SDD em `docs/superpowers/specs/2026-10-21-auth-rbac-refresh-token-design.md` (leia-a) e o código implementado. NÃO invente endpoints/campos — confira contra o que existe.

## Fatos implementados (confira no código, não invente)

### Contrato de token
- Login `POST /api/v1/auth/login` body `{ email, senha }` → `{ accessToken, refreshToken, expiresIn, user: { id, nome, email, roles: [] } }`.
- Refresh `POST /api/v1/auth/refresh` body `{ refreshToken }` → mesmo shape do login (rotação: o refreshToken antigo é invalidado).
- Logout `POST /api/v1/auth/logout` (Bearer) body `{ refreshToken }` → revoga a família daquele refresh.
- Logout-all `POST /api/v1/auth/logout-all` (Bearer) → revoga todos os refresh do usuário.
- Me `GET /api/v1/auth/me` (Bearer) → `{ id, nome, email, roles: [] }`.
- Register `POST /api/v1/auth/register` body `{ nome, email, senha, telefone? }` → cria Cliente.
- Access token: JWT HS256, ~15 min (`Jwt:AccessTokenMinutes`), claims: sub, email, jti, uma `role` por role, uma `perm` por permissão. Refresh: opaco, 7 dias (`Jwt:RefreshTokenDays`), hasheado no banco, rotativo com detecção de reúso (reapresentar refresh revogado revoga a família inteira).

### Catálogo de permissões (12) e roles de sistema
Permissões (recurso:acao): produtos:ler, produtos:escrever, categorias:escrever, clientes:ler, clientes:escrever, pedidos:ler, pedidos:avancar, pagamentos:validar, relatorios:ler, usuarios:gerenciar, roles:gerenciar, permissoes:gerenciar.
Roles: Admin (Sistema, todas), Cliente (Sistema, nenhuma), Gerente (produtos:escrever, categorias:escrever, clientes:ler, clientes:escrever, pedidos:ler, pedidos:avancar, relatorios:ler, pagamentos:validar).

### Rotas administrativas (RBAC)
- /roles (CRUD + PUT /{id}/permissions) → `roles:gerenciar`.
- /permissions (CRUD) → `permissoes:gerenciar`.
- /usuarios (GET, GET/{id}, POST, PUT/{id}/roles, PATCH/{id}/ativo, DELETE soft) → `usuarios:gerenciar`.
- Produtos/Categorias escrita → produtos:escrever/categorias:escrever (GET público). Clientes → clientes:ler/escrever. Relatorios → relatorios:ler. Pagamentos /validar → pagamentos:validar. Pedidos /etapas/avancar → pedidos:avancar.
- Carrinho, checkout, pagamentos do próprio pedido → autenticado (ownership no handler).
- Manutenção: POST /auth/maintenance/purge-tokens → usuarios:gerenciar.

### Regras de negócio
- Nunca deixar o sistema sem admin ativo (bloqueia remover role Admin / desativar / deletar o último admin).
- Roles de sistema (Admin, Cliente) não podem ser alteradas/removidas/ter permissões trocadas.
- DELETE de usuário é soft-delete (Ativo=false).
- Usuário inativo não loga nem faz refresh.
- Housekeeping: BackgroundService remove refresh tokens expirados/revogados fora da retenção (30 dias, a cada 12h) — `Auth:RefreshTokenRetentionDays`, `Auth:CleanupIntervalHours`.

## Entregáveis

### 1. ADRs em `docs/adr/` (formato: Título, Status, Contexto, Decisão, Consequências)
- `0001-rbac-com-permissoes.md` — por que RBAC completo (permissões no token) em vez de roles simples; trade-off da mudança de permissão só valer no próximo refresh.
- `0002-refresh-token-rotativo-postgres.md` — refresh opaco rotativo com detecção de reúso por família, store no Postgres; Redis AVALIADO E ADIADO (gatilho: denylist de access distribuída / múltiplas instâncias / alto volume); por que Postgres basta hoje (Locaweb/Render/Neon).
- `0003-autorizacao-por-permissao-dinamica.md` — policy provider dinâmico (prefixo perm:) em vez de registrar policy por permissão; RequirePermission.
- `0004-limpeza-refresh-tokens.md` — BackgroundService (L1) + endpoint manual (L3) + caso de uso único; agendador externo (Control-M) como evolução futura.
Status das 4: "Aceita".

### 2. Script SQL `docs/sql/seed-roles-permissions.sql`
Espelho do seed da migration: INSERT das 12 permissões, 3 roles, vínculos (Admin=todas, Gerente=8, Cliente=0), idempotente (ON CONFLICT DO NOTHING), nomes de tabela/coluna PascalCase com aspas duplas (Postgres). Cabeçalho comentando que a fonte da verdade é a migration `AuthRbacRefreshToken` e que este script é para conferência/execução manual (ex.: DBeaver via túnel SSH — ver docs/Banco/).

### 3. `docs/Auth/Guia-Permissoes.md` — "Como adicionar uma nova permissão"
Passo a passo ponta a ponta: (1) escolher código recurso:acao; (2) cadastrar a permissão (via POST /permissions com permissoes:gerenciar, OU seed/migration); (3) vincular à(s) role(s) (PUT /roles/{id}/permissions com roles:gerenciar); (4) proteger a rota com `.RequirePermission("recurso:acao")` no endpoint; (5) usuário precisa relogar/refresh para o access token trazer a nova permissão. Nota: permissão sem rota que a verifique é inerte; rota com permissão que ninguém tem nega sempre.

### 4. `docs/Auth/Guia-de-Uso.md` — guia de uso da API de autenticação
Para quem integra o front: fluxo de login (guardar accessToken + refreshToken), usar `Authorization: Bearer <accessToken>`, renovar com /refresh quando o access expira (15 min), tratar 401 (access expirado → refresh; refresh inválido → relogar), logout/logout-all, entender roles vs permissões, exemplos de request/response (JSON) para login/refresh/me. Mencionar que o contrato de login mudou de `{ token, user }` para `{ accessToken, refreshToken, expiresIn, user }`.

### 5. Atualizar `docs/Site/API-Reference.md`
Adicionar a seção de Auth nova (login com refresh, refresh, logout, logout-all) e a matriz de rotas públicas vs. protegidas por permissão, e as novas rotas /roles, /permissions, /usuarios. Confira o formato existente do arquivo e siga-o. Não remova conteúdo correto existente; complemente/atualize o que mudou (ex.: se o login antigo estava documentado como `{ token, user }`, atualize).

## Validação
- Documentos coerentes com o código (confira endpoints/campos/permissões no código antes de escrever).
- `dotnet build GatumsGourmet.sln` deve continuar 0 erros (docs não afetam build, mas confirme que não tocou código por engano).
- Não há testes para docs; apenas revise a consistência você mesmo.

## Commit
`docs: ADRs, scripts de seed e guias de autenticacao`

## Não dispare subagentes. Confira os fatos contra o código — não invente endpoints nem campos.
