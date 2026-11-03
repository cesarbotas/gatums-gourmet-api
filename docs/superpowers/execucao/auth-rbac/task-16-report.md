# Task 16 — Relatório: Documentação (ADRs, seed, guias, API-Reference)

## Resultado
Feature de autenticação RBAC + refresh token documentada. Build da solução 0 erros
(apenas warnings pré-existentes, não relacionados a docs). Somente docs alterados.

## Documentos criados
- `docs/adr/0001-rbac-com-permissoes.md` — RBAC completo com permissões no token; trade-off do stateless (permissão só vale no próximo refresh).
- `docs/adr/0002-refresh-token-rotativo-postgres.md` — refresh opaco rotativo + detecção de reúso por família, store no Postgres; Redis avaliado e adiado com gatilhos (Fase 3).
- `docs/adr/0003-autorizacao-por-permissao-dinamica.md` — `PermissionPolicyProvider` dinâmico (prefixo `perm:`), `RequirePermission`.
- `docs/adr/0004-limpeza-refresh-tokens.md` — BackgroundService (L1) + endpoint manual (L3) + caso de uso único; Control-M como evolução futura.
- `docs/sql/seed-roles-permissions.sql` — espelho idempotente do seed (12 permissões, 3 roles, Admin=12/Gerente=8/Cliente=0), PascalCase com aspas duplas, `ON CONFLICT DO NOTHING`.
- `docs/Auth/Guia-Permissoes.md` — passo a passo para adicionar permissão (código → cadastrar → vincular → `RequirePermission` → relogar).
- `docs/Auth/Guia-de-Uso.md` — guia de uso da auth para o front (login, Bearer, refresh, tratar 401, logout/logout-all, roles vs permissões, JSON de exemplo, mudança de contrato).

## Documento atualizado
- `docs/Site/API-Reference.md` — nova seção "Autorização (RBAC + refresh token)"; seção Auth reescrita (login novo contrato, refresh, logout, logout-all, me, maintenance/purge-tokens); seções `/roles`, `/permissions`, `/usuarios` adicionadas; matriz de rotas pública/Bearer/permissão no fim; todas as anotações legadas `AdminOnly`/`ClienteOnly` substituídas pelas permissões/ownership reais (0 ocorrências remanescentes). Conteúdo correto de Produtos/Categorias/Pedidos/Pagamentos/Relatórios preservado, só a linha de Auth ajustada.

## Como conferi os fatos contra o código (não inventei)
- Contrato de tokens: `LoginResponse.cs` (`AccessToken, RefreshToken, ExpiresIn, User{Id,Nome,Email,Roles[]}`), `LoginHandler.cs` (expiresIn = minutos*60; usuário inativo bloqueado), `RefreshTokenHandler.cs` (rotação, reúso → `RevokeFamilyAsync`, expirado, inativo), `GetCurrentUserResponse.cs` e `RegisterUserResponse.cs` (ambos com `Roles[]`).
- Requests: `LoginRequest(Email,Senha)`, `RefreshTokenRequest(RefreshToken)`, `RegisterUserRequest(Nome,Email,Senha,Telefone?)`, `LogoutRequest(RefreshToken)`.
- Claims do JWT: `JwtTokenGenerator.cs` — `sub`, `email`, `jti`, `ClaimTypes.Role` por role, `"perm"` por permissão (documentei "uma claim de role por role / uma claim `perm` por permissão").
- Rotas e permissões: lidos todos os `*Endpoints.cs` — Auth, Produtos (`produtos:escrever`), Categorias (`categorias:escrever`), Clientes (`clientes:ler`/`clientes:escrever`), Carrinho (`RequireAuthorization`), Pedidos (checkout/concluir `RequireAuthorization`; GET usa `IsAdmin()`; avancar `pedidos:avancar`), Pagamentos (ownership; `/validar` `pagamentos:validar`), Relatórios (`relatorios:ler`), Roles (`roles:gerenciar`), Permissions (`permissoes:gerenciar`), Usuarios (`usuarios:gerenciar`), purge-tokens (`usuarios:gerenciar`). Prefixos `/api/v1/...` confirmados em `EndpointRouteBuilderExtensions.cs`.
- DTOs admin (definidos nos handlers): `CreateRoleRequest(Nome,Descricao?)`, `UpdateRoleRequest(Id,Nome,Descricao?)`, `SetRolePermissionsRequest(RoleId,PermissionIds[])`, `CreatePermissionRequest(Codigo,Descricao?,Recurso,Acao)`, `UpdatePermissionRequest(Id,Descricao?,Recurso,Acao)` (Codigo não muda), `CreateUsuarioRequest(Nome,Email,Senha,Telefone?,RoleIds[])`, `SetUsuarioRolesRequest(UsuarioId,RoleIds[])`, `SetUsuarioAtivoRequest(UsuarioId,Ativo)`. Responses: `RoleResponse`, `PermissionResponse`, `UsuarioResponse`.
- Regras de negócio: `SetUsuarioAtivoHandler`/`SetUsuarioRolesHandler` (nunca sem admin ativo), `UpdateRoleHandler`/`SetRolePermissionsHandler` (roles de sistema imutáveis), soft-delete.
- Seed SQL: copiado 1:1 do bloco `migrationBuilder.Sql(...)` da migration `20261026143118_AuthRbacRefreshToken.cs` — mesmos códigos, descrições, roles e vínculos (Admin via CROSS JOIN, Gerente com as 8 permissões nomeadas, Cliente sem insert).
- `ResultExtensions.cs`: falhas de negócio viram 400; por isso documentei os erros como 400 e distingui 401 (sem token) de 403 (sem permissão) no guia de uso.

## Auto-revisão de consistência
- API-Reference: 0 ocorrências de `AdminOnly`/`ClienteOnly` após a atualização (grep).
- Shapes de request/response documentados batem com os records do código.
- Matriz de rotas no API-Reference consistente com a matriz da SDD e com os endpoints reais.
- Guias referenciam apenas rotas/campos existentes; os exemplos JSON seguem os DTOs.
- Build 0 erros; `git status` mostra só mudanças em `docs/` (nenhum código de produção tocado).

## Concerns
- O guia de uso descreve a estratégia de 401/refresh/retry como recomendação de front; a API em si não força retry — é comportamento do cliente (deixei explícito).
- `GET /pedidos` e `/pedidos/{id}` usam `user.IsAdmin()`. Confirmei em `ClaimsPrincipalExtensions.cs` que `IsAdmin()` = `IsInRole("Admin")` (role por NOME), e NÃO `perm:pedidos:ler` como a SDD sugeria. Corrigi a redação do API-Reference para "role `Admin`" — ponto em que a implementação diverge da SDD (a SDD pretendia basear em `pedidos:ler`; o código ficou em role). Vale um alinhamento futuro se quiserem seguir a SDD à risca.
- Warnings de build pré-existentes (CS0105 em testes, NU1902/NU1903 em `System.Security.Cryptography.Pkcs`) não foram tocados — fora do escopo de docs.
