# SDD — Autenticação com RBAC e Refresh Token

- **Data:** 2026-10-21
- **Branch:** `feature/AjusteLoginToken`
- **Status:** Em revisão
- **Escopo:** Reestruturação de autenticação e autorização da API Gatums Gourmet (.NET 10, Clean Architecture). Substitui o JWT único de 8h por access token curto + refresh token rotativo, e troca o controle de acesso por enum de role (Admin/Cliente) por RBAC completo com permissões, administrável via API.

---

## 1. Objetivo e contexto

### Estado atual (problemas)
- JWT único de **8h hardcoded**, sem refresh token.
- `logout` é no-op (não revoga nada); não há revogação de token.
- Roles são um **enum string** (`UserRole { Admin, Cliente }`) numa coluna de `Usuarios`. Adicionar uma role exige mexer em código (enum, policies, conversão EF).
- Registro público (`/register`) sempre cria Cliente; Admin só via seed.
- Secret de desenvolvimento versionado em `appsettings.json`.

### Objetivo
- **Access token curto** (15 min) + **refresh token rotativo** (7 dias) com detecção de reúso.
- **RBAC completo**: roles e permissões em tabelas; permissões embutidas no access token (autorização stateless).
- **Gestão via API** (U2): CRUD de roles, CRUD de permissões, gestão de usuários e vínculos.
- **Revisão de rotas públicas**.
- Logout real e "revogar todas as sessões".

### Premissas
- Projeto hoje é usado **apenas pelo desenvolvedor**; não há cliente externo consumindo contratos. Logo: sem necessidade de V2 para compatibilidade; o `/api/v1/auth/login` é alterado direto para o novo formato.
- Access 15 min / refresh 7 dias, **configuráveis** via `appsettings`.
- Refresh trafega no **corpo da requisição** (SPA desacoplado), não em cookie HttpOnly.
- Store de refresh no **Postgres** (igual em Locaweb/Render/Neon), sem Redis agora.

### Princípios transversais (aplicados a todo o design)
- **Uma classe por arquivo**, agrupadas por pasta de responsabilidade. Nada de múltiplos services/repositories num só arquivo.
- Interfaces de repositório em `Domain/Repositories/` (uma por arquivo); interfaces de service em `Domain/Services/Interfaces/`.
- Autorização o mais **dinâmica** possível (policy provider que cria policies sob demanda).
- Tudo documentado (SDD + ADRs + guia de permissões + guia de uso).

### Fora de escopo (fases futuras, só registradas como contexto)
- **CORS por ambiente** (Fase 2) — hoje não há API Gateway; será configurado na própria API.
- **Redis** para denylist de access token / escala (Fase 3) — adiado, com gatilho documentado na ADR 0002.
- **Fluxo de pedido:** adicionar **cancelamento** e corrigir o **acoplamento frágil pagamento↔etapa** (validar transação aprovada de verdade antes de avançar). Sem etapa de cozinha (produtos pré-prontos). Pagamento recusado/expirado = depois.
- **Integração MercadoPago** (gateway real + webhook assíncrono).
- **Agendador externo** (estilo Control-M) para orquestrar rotinas de manutenção.

---

## 2. Modelo de dados

Novas entidades em `Domain/Entities/Auth/` (uma por arquivo):

### Role
`Id (int, identity)`, `Nome (unique)`, `Descricao`, `Sistema (bool)`.
Roles de sistema (Admin, Cliente) não podem ser renomeadas nem deletadas.

### Permission
`Id (int, identity)`, `Codigo (unique, formato recurso:acao)`, `Descricao`, `Recurso`, `Acao`.

### RolePermission (junção)
`RoleId`, `PermissionId`. PK composta.

### UsuarioRole (junção)
`UsuarioId`, `RoleId`. PK composta. Permite N roles por usuário.

### RefreshToken
`Id (uuid)`, `UsuarioId`, `TokenHash`, `FamiliaId (uuid)`, `ExpiraEm`, `CriadoEm`, `Revogado (bool)`, `RevogadoEm?`, `SubstituidoPorTokenHash?`, `MotivoRevogacao?`, `CriadoPorIp?`.

### Mudança em Usuario
- Remove o campo escalar `Role (UserRole)`.
- Adiciona navegação `ICollection<UsuarioRole>` e `Ativo (bool)`.
- O enum `UserRole` é **aposentado** (vira dado em tabela).

### Relações
```
Usuarios ──< UsuarioRoles >── Roles ──< RolePermissions >── Permissions
Usuarios ──< RefreshTokens
```

### Repositórios / stores (interfaces no Domain, impl na Infrastructure; 1 por arquivo)
- `IRoleRepository` / `RoleRepository`
- `IPermissionRepository` / `PermissionRepository`
- `IRefreshTokenStore` / `RefreshTokenStore` (inclui `DeleteExpiredAsync(cutoff, ct)` para housekeeping)
- `IUsuarioRepository` ampliado (listar com roles, vincular roles, ativar/desativar)

### Configurações EF (Infrastructure/Data/Configurations/Auth/, 1 por arquivo)
`RoleConfiguration`, `PermissionConfiguration`, `RolePermissionConfiguration`, `UsuarioRoleConfiguration`, `RefreshTokenConfiguration`.
Índices únicos: `Role.Nome`, `Permission.Codigo`, `RefreshToken.TokenHash`.

---

## 3. Catálogo de permissões e matriz de rotas

### Catálogo inicial (formato `recurso:acao`)
| Código | Descrição |
|---|---|
| `produtos:ler` | Ver produtos (reservado; catálogo é público hoje) |
| `produtos:escrever` | Criar/editar/excluir produtos |
| `categorias:escrever` | Criar/editar/excluir categorias |
| `clientes:ler` | Listar/ver clientes (CRM) |
| `clientes:escrever` | Criar/editar/excluir clientes |
| `pedidos:ler` | Ver todos os pedidos |
| `pedidos:avancar` | Avançar etapas do pedido |
| `pagamentos:validar` | Validar pagamento para aprovação |
| `relatorios:ler` | Ver relatórios de vendas |
| `usuarios:gerenciar` | Criar usuários e atribuir roles |
| `roles:gerenciar` | CRUD de roles e vínculo role↔permissão |
| `permissoes:gerenciar` | CRUD do catálogo de permissões (para backoffice futuro) |

### Roles de sistema no seed
- **Admin** (`Sistema=true`): todas as permissões.
- **Cliente** (`Sistema=true`): nenhuma permissão administrativa (só ownership).
- **Gerente**: `produtos:escrever`, `categorias:escrever`, `clientes:ler`, `clientes:escrever`, `pedidos:ler`, `pedidos:avancar`, `relatorios:ler`, `pagamentos:validar`.

### Dois tipos de autorização
- **Permissão** — operações administrativas (`RequirePermission("...")`).
- **Ownership autenticado** — cliente mexendo no que é dele (carrinho, próprios pedidos, pagamentos dos próprios pedidos): `RequireAuthorization()` + verificação `recurso.UsuarioId == userId` no handler. NÃO vira permissão.

### Matriz de rotas (proposto)
| Rota | Proposto |
|---|---|
| POST /auth/login | pública |
| POST /auth/register | pública (cria Cliente) |
| POST /auth/refresh | pública (valida o refresh) |
| POST /auth/logout | autenticado (revoga refresh) |
| POST /auth/logout-all | autenticado (revoga todos os refresh do usuário) |
| GET /auth/me | autenticado |
| GET /produtos, GET /produtos/{id} | pública (catálogo) |
| POST/PUT/DELETE /produtos | `produtos:escrever` |
| GET /categorias, /categorias/{id} | pública |
| POST/PUT/DELETE /categorias | `categorias:escrever` |
| GET /clientes | `clientes:ler` |
| POST/PUT/DELETE /clientes | `clientes:escrever` |
| carrinho (todas) | autenticado + ownership |
| POST /pedidos/checkout | autenticado + ownership |
| GET /pedidos | autenticado (com `pedidos:ler` vê todos; senão os próprios) |
| GET /pedidos/{id} | autenticado; acesso se for o dono (`UsuarioId == userId`) OU tiver `pedidos:ler` |
| POST /pedidos/{id}/etapas/avancar | `pedidos:avancar` |
| POST /pedidos/{id}/pagamento/concluir | autenticado + ownership |
| pagamentos do próprio pedido (opcoes, iniciar, cartao, status, pix) | autenticado + ownership |
| POST /pagamentos/{id}/validar | `pagamentos:validar` |
| GET /relatorios/vendas/resumo | `relatorios:ler` |
| CRUD /roles | `roles:gerenciar` |
| CRUD /permissions | `permissoes:gerenciar` |
| CRUD /usuarios | `usuarios:gerenciar` |

### Rotas públicas (conjunto final)
`login`, `register`, `refresh` e o catálogo de leitura (`GET /produtos`, `GET /produtos/{id}`, `GET /categorias`, `GET /categorias/{id}`). Todo o resto exige autenticação.

---

## 4. Fluxo de tokens

### Login (`POST /auth/login`)
1. Valida email+senha (BCrypt).
2. Resolve permissões efetivas (união das permissões de todas as roles) via `IPermissionResolver`.
3. Gera **access token** (JWT HS256, 15 min) com claims: `sub`, `email`, `role[]` (informativo), `perm[]`, `jti`, `exp`.
4. Gera **refresh token** opaco (32 bytes aleatórios, base64url). Grava só o **hash** em `RefreshTokens` com `ExpiraEm`, `FamiliaId`, `CriadoPorIp`.
5. Retorna `{ accessToken, refreshToken, expiresIn, user{...} }`.

### Refresh (`POST /auth/refresh`, público)
1. Hasheia e busca o token.
2. Valida: existe? não expirado? não revogado?
3. **Detecção de reúso:** se o token achado já está revogado/substituído (refresh antigo reapresentado) → revoga a **família inteira** e retorna erro (força relogin).
4. Caminho normal — **rotação**: marca o atual como revogado (`SubstituidoPor` → novo), emite refresh novo (mesma `FamiliaId`) e access novo com permissões **recalculadas**.

### Logout / Logout-all
- `POST /auth/logout` (autenticado): revoga o refresh informado.
- `POST /auth/logout-all` (autenticado): revoga todos os refresh ativos do usuário (útil pós-troca de senha).
- O access token continua válido até expirar (15 min) — comportamento esperado de JWT stateless.

### Conceito de família
Refresh tokens que descendem de um mesmo login compartilham `FamiliaId`. Rotação encadeia A→B→C. Reapresentar um token já rotacionado revoga a família toda (padrão OWASP *refresh token rotation with reuse detection*).

### Componentes novos (1 por arquivo)
- `IRefreshTokenGenerator` / `RefreshTokenGenerator` (valor aleatório + hash).
- `IPermissionResolver` / `PermissionResolver` (permissões efetivas do usuário).
- `JwtTokenGenerator` — nova assinatura recebendo roles + permissões.
- Config: `Jwt:AccessTokenMinutes`, `Jwt:RefreshTokenDays`.

### Trade-off aceito
Mudança de permissão de uma role reflete só no **próximo refresh** (access é stateless). Com access de 15 min, a janela é curta. Revogação imediata de access = Fase 3 (Redis).

---

## 5. Autorização por permissão (dinâmica)

Policy provider dinâmico (padrão ASP.NET Core), na camada Api sob `Authorization/` (1 por arquivo):
- `PermissionRequirement.cs` — carrega o código da permissão.
- `PermissionAuthorizationHandler.cs` — aprova se a claim `perm[]` contém o código exigido.
- `PermissionPolicyProvider.cs` — cria policies `perm:*` sob demanda (não precisa registrar cada permissão no startup).
- `RequirePermissionExtensions.cs` — o `.RequirePermission("...")` para os endpoints.

Ownership permanece fora de permissão: `RequireAuthorization()` + checagem no handler. O helper `IsAdmin()` atual (usado em pedidos para decidir se vê todos) passa a basear-se em `perm:pedidos:ler`.

As policies legadas `AdminOnly`/`ClienteOnly` e os `RequireRole` são removidos.

---

## 6. Endpoints de administração (U2)

Grupos novos em `Endpoints/V1/` (1 caso de uso por operação, pasta própria):

### /api/v1/roles — `roles:gerenciar`
`GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}` (bloqueia `Sistema=true` ou com usuários vinculados), `PUT /{id}/permissions` (define o conjunto).

### /api/v1/permissions — `permissoes:gerenciar`
`GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`. CRUD completo (backoffice futuro). Nota: permissão sem rota que a verifique é **inerte** (não quebra nada, só não tem efeito).

### /api/v1/usuarios — `usuarios:gerenciar`
`GET /`, `GET /{id}`, `POST /` (cria usuário já com roles), `PUT /{id}/roles` (define roles), `PATCH /{id}/ativo` (ativa/desativa), `DELETE /{id}` (**soft-delete** → `Ativo=false`, preserva histórico de pedidos).

### Regras de negócio de segurança
- **Nunca deixar o sistema sem admin**: bloqueia remover a última role Admin, desativar ou excluir o último admin ativo.
- Usuário com `Ativo=false` não loga nem obtém refresh.

### Ajustes em /api/v1/auth
`login` (novo formato), `logout` (revoga), `logout-all` (novo), `refresh` (novo), `register` e `me` mantidos.

---

## 7. Migração e deploy

Produção roda migrations automaticamente no startup (`DatabaseExtensions`/`DbSeeder`).

### Migration EF única, transacional
1. Cria `Roles`, `Permissions`, `RolePermissions`, `UsuarioRoles`, `RefreshTokens` + coluna `Ativo` em `Usuarios` + índices únicos.
2. **Seed via migration**: 3 roles de sistema + catálogo de permissões + vínculos role→permissão.
3. **Data migration**: para cada usuário existente, copia a role antiga (coluna `Role`) para `UsuarioRoles`; define `Ativo=true`.
4. Só então **remove a coluna `Role`**.
Tudo numa transação: falha em qualquer passo = rollback, API não sobe com schema parcial.

### Divisão seed
- **Migration**: roles e permissões de sistema (garante existência no momento da mudança de schema).
- **DbSeeder**: apenas o usuário admin inicial (`admin@gatums.com`), agora vinculando à role Admin via tabela; mantém a checagem `AnyAsync` para não duplicar.

### Tokens na virada
Sem retrocompatibilidade de token antigo (troca de esquema de auth). Projeto usado só pelo desenvolvedor → **relogin no deploy é aceitável**. Janela mínima (access 15 min + restart do processo).

### Rollback
`Down` recria a coluna `Role`, copia de volta a role de maior privilégio de cada usuário e dropa as tabelas novas. Documentado: rollback perde roles múltiplas (esperado ao voltar ao modelo antigo).

---

## 8. Housekeeping (limpeza de refresh tokens)

Caso de uso dedicado em `UseCases/V1/Manutencao/PurgeExpiredRefreshTokens/` (separado dos use cases de auth).

- **L1 — BackgroundService** (`RefreshTokenCleanupService : BackgroundService` na Api): roda em intervalo configurável e apaga tokens expirados/revogados fora da retenção.
- **L3 — Endpoint manual** (`POST /auth/maintenance/purge-tokens`, `usuarios:gerenciar`): gatilho manual; usa o mesmo caso de uso.
- Config: `Auth:RefreshTokenRetentionDays` (30), `Auth:CleanupIntervalHours` (12).
- Store: `IRefreshTokenStore.DeleteExpiredAsync(cutoff, ct)`.
- Futuro (registrado): agendador externo (Control-M) para orquestrar.

---

## 9. Documentação (entregável desta fase)

- **Esta SDD**: `docs/superpowers/specs/2026-10-21-auth-rbac-refresh-token-design.md`.
- **ADRs** (`docs/adr/`, 1 por arquivo, Contexto/Decisão/Consequências/Status):
  - `0001-rbac-com-permissoes.md`
  - `0002-refresh-token-rotativo-postgres.md` (Redis adiado + gatilho)
  - `0003-autorizacao-por-permissao-dinamica.md`
  - `0004-limpeza-refresh-tokens.md`
- **Scripts** (`docs/sql/seed-roles-permissions.sql`): espelho do seed da migration, para conferência manual via DBeaver.
- **Guia de permissões** (`docs/Auth/Guia-Permissoes.md`): como adicionar uma nova permissão de ponta a ponta.
- **Guia de uso** (`docs/Auth/Guia-de-Uso.md`): como logar, usar access token, renovar com refresh, entender roles.
- **Atualização** de `docs/Site/API-Reference.md`: nova seção de auth + matriz de rotas.

---

## 10. Estrutura de pastas (layout alvo)

```
Domain/
  Entities/Auth/            Role.cs, Permission.cs, RolePermission.cs, UsuarioRole.cs, RefreshToken.cs, Usuario.cs
  Repositories/             IRoleRepository.cs, IPermissionRepository.cs, IRefreshTokenStore.cs
  Services/Interfaces/      IJwtTokenGenerator.cs, IRefreshTokenGenerator.cs, IPermissionResolver.cs

Infrastructure/
  Services/Auth/            JwtTokenGenerator.cs, RefreshTokenGenerator.cs, BcryptPasswordHasher.cs, PermissionResolver.cs
  Data/Configurations/Auth/ RoleConfiguration.cs, PermissionConfiguration.cs, RolePermissionConfiguration.cs, UsuarioRoleConfiguration.cs, RefreshTokenConfiguration.cs
  Data/Repositories/Auth/   RoleRepository.cs, PermissionRepository.cs, RefreshTokenStore.cs
  Migrations/               <timestamp>_AuthRbacRefreshToken.cs

Application/
  UseCases/V1/Auth/         Login/, RefreshToken/, Logout/, LogoutAll/, GetCurrentUser/, RegisterUser/
  UseCases/V1/Roles/        CreateRole/, ListRoles/, GetRoleById/, UpdateRole/, DeleteRole/, SetRolePermissions/
  UseCases/V1/Permissions/  ListPermissions/, GetPermissionById/, CreatePermission/, UpdatePermission/, DeletePermission/
  UseCases/V1/Usuarios/     CreateUsuario/, ListUsuarios/, GetUsuarioById/, SetUsuarioRoles/, SetUsuarioAtivo/, DeleteUsuario/
  UseCases/V1/Manutencao/   PurgeExpiredRefreshTokens/

Api/
  Authorization/            PermissionRequirement.cs, PermissionAuthorizationHandler.cs, PermissionPolicyProvider.cs, RequirePermissionExtensions.cs
  Endpoints/V1/             AuthEndpoints.cs, RolesEndpoints.cs, PermissionsEndpoints.cs, UsuariosEndpoints.cs (+ ajustes nos existentes)
  BackgroundServices/       RefreshTokenCleanupService.cs
```

---

## 11. Testes

- **Unitários**: `PermissionResolver`, `RefreshTokenGenerator` (hash), detecção de reúso (revogação de família), regras de "nunca sem admin", geração de token com claims corretas.
- **Integração**: fluxo login → acesso a rota protegida por permissão → refresh (rotação) → reúso detectado (403 + família revogada) → logout. CRUD de roles/permissions/usuarios com as permissões certas. Rotas públicas acessíveis sem token.
- Cobertura dos handlers novos seguindo o padrão existente (xUnit + Moq + FluentAssertions).
