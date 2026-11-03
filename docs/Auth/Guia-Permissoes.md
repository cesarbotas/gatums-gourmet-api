# Guia — Como adicionar uma nova permissão

Este guia descreve, de ponta a ponta, como introduzir uma nova permissão no RBAC
da API Gatums Gourmet. O modelo de autorização é explicado na
[ADR 0001](../adr/0001-rbac-com-permissoes.md) e na
[ADR 0003](../adr/0003-autorizacao-por-permissao-dinamica.md).

## Visão geral

Uma permissão é um código no formato **`recurso:acao`** (ex.: `pedidos:avancar`).
Rotas administrativas são protegidas por `.RequirePermission("recurso:acao")`.
O access token do usuário carrega, como claims `perm`, todas as permissões
efetivas (a união das permissões de todas as suas roles). A autorização é
**stateless**: o servidor só compara o código exigido pela rota com as claims
`perm` do token.

Para uma nova permissão passar a ter efeito, 4 coisas precisam existir:

1. A permissão no catálogo (`Permissions`).
2. O vínculo da permissão com a(s) role(s) que devem tê-la (`RolePermissions`).
3. A rota exigindo a permissão (`.RequirePermission("...")`).
4. O usuário com um token **novo** que já traga a permissão (relogar/refresh).

## Passo a passo

### 1. Escolha o código `recurso:acao`

Siga a convenção existente: `recurso` no plural (`produtos`, `pedidos`,
`relatorios`), `acao` como verbo curto (`ler`, `escrever`, `avancar`,
`validar`, `gerenciar`). O código precisa conter `:` (validado na criação).

Exemplo: para permitir cancelar pedidos, use `pedidos:cancelar`.

### 2. Cadastre a permissão

Duas vias:

**a) Via API** (runtime, exige a permissão `permissoes:gerenciar`):

```http
POST /api/v1/permissions
Authorization: Bearer <access token com permissoes:gerenciar>
Content-Type: application/json

{
  "codigo": "pedidos:cancelar",
  "recurso": "pedidos",
  "acao": "cancelar",
  "descricao": "Cancelar pedidos"
}
```

Resposta `201`: `{ "id": 13 }`.

**b) Via seed/migration** (permanente, entra em todo ambiente): adicione a linha
ao `INSERT INTO "Permissions"` da migration `AuthRbacRefreshToken` **e** ao
espelho em [`docs/sql/seed-roles-permissions.sql`](../sql/seed-roles-permissions.sql).
Prefira esta via quando a permissão faz parte do catálogo base do produto.

### 3. Vincule a permissão à(s) role(s)

Defina o conjunto de permissões de uma role com `PUT /roles/{id}/permissions`
(exige `roles:gerenciar`). O corpo envia a **lista completa** de `permissionIds`
que a role deve ter (substitui o conjunto atual, não é incremental):

```http
PUT /api/v1/roles/3/permissions
Authorization: Bearer <access token com roles:gerenciar>
Content-Type: application/json

{ "permissionIds": [2, 3, 6, 7, 13] }
```

> **Roles de sistema (Admin, Cliente) têm conjunto fixo** e não aceitam essa
> operação (retorna erro). Se a nova permissão precisa valer para o Admin,
> adicione o vínculo no seed/migration (o Admin recebe todas as permissões via
> `CROSS JOIN` no seed).

### 4. Proteja a rota

No endpoint correspondente, exija a permissão:

```csharp
group.MapPost("/{id:guid}/cancelar", async (/* ... */) => /* ... */)
    .RequirePermission("pedidos:cancelar");
```

Não é preciso registrar nada no startup: o `PermissionPolicyProvider` cria a
policy `perm:pedidos:cancelar` sob demanda (ver ADR 0003).

### 5. O usuário precisa de um token novo

Como a autorização é stateless, a permissão recém-concedida **só aparece no
próximo access token**. O usuário deve:

- fazer `POST /auth/refresh` (renova o access com permissões recalculadas), ou
- relogar (`POST /auth/login`).

Enquanto o usuário usar um access antigo (até ~15 min), a nova permissão ainda
não vale para ele.

## Notas importantes

- **Permissão inerte:** uma permissão cadastrada mas que nenhuma rota verifica
  não causa erro — simplesmente não tem efeito até alguma rota usá-la.
- **Rota que nega sempre:** uma rota protegida por uma permissão que ninguém
  possui vai negar (403) todas as requisições até que alguma role a conceda.
- **Erro de digitação:** o código da rota (`"pedidos:cancelr"`) e o código
  cadastrado precisam bater exatamente; divergência vira 403 em runtime.
- **Ownership não é permissão:** acesso do cliente ao que é dele (carrinho,
  próprios pedidos) continua sendo `RequireAuthorization()` + checagem no
  handler, e **não** deve virar permissão.
