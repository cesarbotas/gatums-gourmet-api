# ADR 0003 — Autorização por permissão com policy provider dinâmico

- **Status:** Aceita
- **Data:** 2026-10-26
- **Contexto da feature:** Autenticação RBAC + refresh token (branch `feature/AjusteLoginToken`)

## Contexto

Com o RBAC (ADR 0001), cada rota administrativa precisa exigir uma permissão
específica (ex.: `produtos:escrever`, `roles:gerenciar`). No modelo padrão do
ASP.NET Core, cada policy nomeada precisa ser **registrada no startup**
(`AddAuthorization(o => o.AddPolicy(...))`).

Registrar uma policy por permissão tem dois problemas:

- É repetitivo e acopla o startup ao catálogo de permissões: 12 permissões hoje,
  mais a cada nova.
- Como permissões podem ser **criadas em runtime** (via `POST /permissions`), o
  catálogo não é fixo em tempo de compilação — registrar tudo no startup não
  acompanha o que existe no banco.

As policies legadas `AdminOnly`/`ClienteOnly` e os `RequireRole` não encaixam no
modelo granular e seriam removidos.

## Decisão

Implementar um **policy provider dinâmico** que cria policies de permissão sob
demanda, na camada Api sob `Authorization/` (uma classe por arquivo):

- **`PermissionPolicyProvider`** — `IAuthorizationPolicyProvider` que, ao receber
  um nome de policy com o prefixo **`perm:`**, cria sob demanda uma policy com
  `RequireAuthenticatedUser()` + `PermissionRequirement(codigo)`. Nomes sem o
  prefixo são delegados ao `DefaultAuthorizationPolicyProvider` (fallback).
- **`PermissionRequirement`** — carrega o código da permissão exigida.
- **`PermissionAuthorizationHandler`** — aprova se a claim `perm` do usuário
  contém o código exigido; caso contrário, nega.
- **`RequirePermissionExtensions`** — açúcar sintático `.RequirePermission("recurso:acao")`
  que, por baixo, chama `RequireAuthorization("perm:recurso:acao")`.

Assim, proteger uma rota é só:

```csharp
group.MapPost("/", /* ... */).RequirePermission("produtos:escrever");
```

Nenhuma permissão precisa ser pré-registrada no startup: a policy `perm:...` é
materializada na primeira vez que a rota correspondente é acessada.

## Consequências

**Positivas**
- Adicionar uma permissão nova não exige mexer no startup: basta criar a
  permissão, vincular à role e usar `.RequirePermission("...")` no endpoint
  (ver `docs/Auth/Guia-Permissoes.md`).
- Startup limpo, desacoplado do catálogo de permissões.
- Decisão de acesso é **stateless** — o handler só lê a claim `perm` do token, sem
  consulta ao banco por requisição.
- `RequirePermission` deixa a intenção explícita e legível no endpoint.

**Negativas / trade-offs**
- Um erro de digitação no código da permissão (ex.: `produto:escrever` em vez de
  `produtos:escrever`) só aparece em runtime como um 403, não em tempo de
  compilação. Mitiga-se mantendo os códigos no catálogo documentado.
- A checagem depende do que está no token: uma permissão concedida só vale após o
  próximo refresh/relogin (consequência do stateless, ver ADR 0001).
- As antigas `AdminOnly`/`ClienteOnly`/`RequireRole` foram removidas; qualquer
  referência externa a esses nomes deixou de existir (não há consumidor externo).
