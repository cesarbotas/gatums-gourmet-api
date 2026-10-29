# Task 2 — Relatório: Interfaces de repositório e service (Domain)

**Branch:** feature/AjusteLoginToken
**Commit:** `301f9ff` — feat: interfaces de repositorio e service para RBAC/refresh
**Data:** 2026-10-26

## O que foi feito

Criadas 5 interfaces novas e modificadas 2 existentes na camada Domain, seguindo
o brief verbatim. Uma interface por arquivo, `net10.0`, `nullable enable` (herdado
do csproj), namespaces conforme pasta.

### Arquivos CRIADOS (5)
| Arquivo | Namespace | Conteúdo |
|---|---|---|
| `src/GatumsGourmet.Domain/Repositories/IRoleRepository.cs` | `...Repositories` | CRUD de Role + `SetPermissionsAsync` + `CountUsuariosAsync` + `SaveChangesAsync` |
| `src/GatumsGourmet.Domain/Repositories/IPermissionRepository.cs` | `...Repositories` | CRUD de Permission (`GetByIdAsync`, `GetByCodigoAsync`, `ListAsync`, `AddAsync`, `Update`, `Remove`, `SaveChangesAsync`) |
| `src/GatumsGourmet.Domain/Repositories/IRefreshTokenStore.cs` | `...Repositories` | `AddAsync`, `GetByHashAsync`, `RevokeFamilyAsync`, `RevokeAllForUserAsync`, `DeleteExpiredAsync`, `SaveChangesAsync` |
| `src/GatumsGourmet.Domain/Services/Interfaces/IRefreshTokenGenerator.cs` | `...Services.Interfaces` | `(string PlainToken, string Hash) Generate()` + `string Hash(string plainToken)` |
| `src/GatumsGourmet.Domain/Services/Interfaces/IPermissionResolver.cs` | `...Services.Interfaces` | `ResolveAsync(Guid usuarioId, ...)` retornando `(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissoes)` |

### Arquivos MODIFICADOS (2)
| Arquivo | Mudança |
|---|---|
| `src/GatumsGourmet.Domain/Services/Interfaces/IJwtTokenGenerator.cs` | Assinatura trocada de `Generate(Guid, string email, string role)` para `Generate(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissoes)` |
| `src/GatumsGourmet.Domain/Repositories/IUsuarioRepository.cs` | Mantidos os 4 métodos existentes (`GetByEmailAsync`, `GetByIdAsync`, `AddAsync`, `SaveChangesAsync`); adicionados `ListAsync`, `GetByIdComRolesAsync`, `SetRolesAsync`, `SetAtivoAsync`, `CountAdminsAtivosAsync` |

## Resultado do build do Domain

`dotnet build src/GatumsGourmet.Domain` → **êxito, 0 erros, 0 warnings** (exit code 0).
Build da solução inteira NÃO foi executado (Application quebrada de propósito desde a Task 1, conforme instrução).

## Auto-revisão

- **Uma interface por arquivo:** OK — cada um dos 7 arquivos contém exatamente uma interface.
- **Assinaturas verbatim:** OK — conferidas contra o brief método a método (inclusive tuplas nomeadas e `CancellationToken ct = default`).
- **Localização correta:** repositórios em `Repositories/`, services em `Services/Interfaces/`.
- **Padrão do repo:** `using GatumsGourmet.Domain.Entities.Auth;` só onde há tipo de entidade (Role/Permission/RefreshToken/Usuario); interfaces de service puras (IRefreshTokenGenerator, IPermissionResolver) não têm using de entidade, igual ao IJwtTokenGenerator existente. File-scoped namespace, sem chaves extras — alinhado ao padrão existente.
- **noBOM:** confirmado via leitura dos bytes iniciais (`117,115,105` = "usi"; `110,97,109` = "nam"), sem EF BB BF.
- **Staging seletivo:** `git add` dos 7 arquivos específicos; `git status` confirmou 5 `A` + 2 `M`, nada mais.

## Concerns

- O brief menciona no cabeçalho "modificar ... IUsuarioRole (ampliar)", mas a seção "Arquivos a MODIFICAR" especifica `IUsuarioRepository.cs` (com os métodos de usuário). Interpretado como `IUsuarioRepository` — que é o alvo coerente com a lista de métodos fornecida. Não existe interface `IUsuarioRole` (apenas a entidade `UsuarioRole`).
- A troca de assinatura de `IJwtTokenGenerator` quebra `LoginHandler` e a impl `JwtTokenGenerator` — quebra **intencional e esperada**, a ser corrigida nas Tasks 3 e 7. Consumidores não foram tocados, conforme instrução.
- Aviso do git sobre LF→CRLF nos arquivos novos (normalização de linha do working copy no Windows); não afeta o conteúdo versionado nem o build.
