# ADR 0001 — RBAC com permissões no access token

- **Status:** Aceita
- **Data:** 2026-10-26
- **Contexto da feature:** Autenticação RBAC + refresh token (branch `feature/AjusteLoginToken`)

## Contexto

O controle de acesso original era um enum de role (`UserRole { Admin, Cliente }`)
gravado numa coluna escalar de `Usuarios`. Esse modelo tinha limitações:

- Adicionar uma nova role exigia mexer em código: alterar o enum, as policies
  (`AdminOnly`/`ClienteOnly`), a conversão EF e o seed.
- Não havia granularidade: ou o usuário era Admin (podia tudo) ou Cliente (não
  podia nada administrativo). Não dava para conceder, por exemplo, apenas
  "avançar pedidos" sem conceder acesso total.
- Não era administrável em runtime — qualquer ajuste de acesso passava por deploy.

O sistema precisava de granularidade por operação e de gestão de acesso via API,
sem depender de deploy a cada mudança de política.

## Decisão

Adotar **RBAC completo** com roles e permissões persistidas em tabelas:

- `Roles` (`Id`, `Nome` único, `Descricao`, `Sistema`).
- `Permissions` (`Id`, `Codigo` único no formato `recurso:acao`, `Descricao`,
  `Recurso`, `Acao`).
- `RolePermissions` (junção N:N entre role e permissão).
- `UsuarioRoles` (junção N:N — um usuário pode ter várias roles).

O catálogo inicial tem **12 permissões** (`produtos:ler`, `produtos:escrever`,
`categorias:escrever`, `clientes:ler`, `clientes:escrever`, `pedidos:ler`,
`pedidos:avancar`, `pagamentos:validar`, `relatorios:ler`, `usuarios:gerenciar`,
`roles:gerenciar`, `permissoes:gerenciar`) e **3 roles de sistema** no seed:

- **Admin** (`Sistema=true`): todas as 12 permissões.
- **Cliente** (`Sistema=true`): nenhuma permissão administrativa.
- **Gerente** (`Sistema=false`): 8 permissões operacionais (`produtos:escrever`,
  `categorias:escrever`, `clientes:ler`, `clientes:escrever`, `pedidos:ler`,
  `pedidos:avancar`, `relatorios:ler`, `pagamentos:validar`).

As **permissões efetivas** de um usuário (união das permissões de todas as suas
roles) são resolvidas no login/refresh por `IPermissionResolver` e **embutidas no
access token** como claims `perm` (uma por permissão). A autorização de rota é,
assim, **stateless**: o handler de autorização apenas inspeciona as claims do
token, sem ir ao banco a cada requisição.

Dois tipos de autorização convivem:

- **Permissão** — operações administrativas, protegidas por
  `.RequirePermission("recurso:acao")`.
- **Ownership autenticado** — cliente mexendo no que é dele (carrinho, próprios
  pedidos, pagamentos dos próprios pedidos): `RequireAuthorization()` + checagem
  `recurso.UsuarioId == userId` no handler. Ownership **não** vira permissão.

As roles de sistema (Admin, Cliente) são imutáveis: não podem ser renomeadas,
excluídas, nem ter o conjunto de permissões alterado via API.

## Consequências

**Positivas**
- Granularidade por operação; dá para montar roles sob medida (ex.: Gerente).
- Gestão em runtime via API (`/roles`, `/permissions`, `/usuarios`) sem deploy.
- Autorização stateless e barata (sem consulta ao banco por requisição).
- Um usuário pode acumular várias roles (N:N).

**Negativas / trade-offs**
- **A mudança de permissão de uma role só reflete no próximo access token.**
  Como o access é stateless e vive ~15 min, conceder ou revogar uma permissão só
  tem efeito quando o usuário fizer o próximo refresh (ou relogar). A janela é
  curta por design (access curto), mas existe.
- Revogação **imediata** de um access token em curso não é possível neste modelo.
  Isso é consequência direta de JWT stateless e está endereçado como evolução
  futura (ADR 0002 — denylist distribuída com Redis, adiada).
- Um token com muitas permissões fica maior; com 12 permissões o custo é
  irrelevante, mas é um fator a observar se o catálogo crescer muito.

**Notas de implementação**
- As roles entram no token como claims padrão de role (`ClaimTypes.Role`) e são
  **informativas**; a decisão de acesso administrativo é feita por `perm`.
- Uma permissão sem nenhuma rota que a verifique é **inerte** (não quebra nada).
  Uma rota que exige uma permissão que ninguém tem **nega sempre**.
