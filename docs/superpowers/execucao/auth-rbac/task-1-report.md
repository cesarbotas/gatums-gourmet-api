# Relatório — Task 1: Entidades de domínio do RBAC e refresh token

**Branch:** `feature/AjusteLoginToken`
**Projeto alvo:** `src/GatumsGourmet.Domain` (camada sem dependências de projeto)

## O que foi implementado

### Entidades criadas (1 classe por arquivo) em `src/GatumsGourmet.Domain/Entities/Auth/`
- `Role.cs` — `Id`, `Nome`, `Descricao?`, `Sistema`, coleções `RolePermissions` e `UsuarioRoles` (inicializadas com `[]`).
- `Permission.cs` — `Id`, `Codigo` (formato `recurso:acao`), `Descricao?`, `Recurso`, `Acao`, coleção `RolePermissions`.
- `RolePermission.cs` — junção `Role`×`Permission` (`RoleId`, `Role`, `PermissionId`, `Permission`).
- `UsuarioRole.cs` — junção `Usuario`×`Role` (`UsuarioId`, `Usuario`, `RoleId`, `Role`).
- `RefreshToken.cs` — `Id`, `UsuarioId`, `TokenHash`, `FamiliaId`, `ExpiraEm`, `CriadoEm`, `Revogado`, `RevogadoEm?`, `SubstituidoPorTokenHash?`, `MotivoRevogacao?`, `CriadoPorIp?`.

Todas verbatim conforme o brief.

### Entidade modificada
- `src/GatumsGourmet.Domain/Entities/Auth/Usuario.cs`:
  - Removida a propriedade `public UserRole Role { get; set; }`.
  - Removido o `using GatumsGourmet.Domain.Enums.Auth;`.
  - Adicionado `public bool Ativo { get; set; }`.
  - Adicionado `public ICollection<UsuarioRole> UsuarioRoles { get; set; } = [];`.
  - Mantidos `Id`, `Nome`, `Email`, `Telefone`, `SenhaHash`, `CriadoEm` e o construtor vazio.

### Teste (TDD)
- `tests/GatumsGourmet.UnitTests/Domain/Entities/Auth/RefreshTokenTests.cs`:
  - `Novo_refresh_token_nao_nasce_revogado`: instancia `new RefreshToken()` e assevera `Revogado == false` e `RevogadoEm == null`. Usa FluentAssertions + xUnit, seguindo o padrão do projeto (ver `PedidoWorkflowServiceTests`).

## Build / Teste — resultados e evidência TDD

### RED (antes das entidades)
`dotnet test tests/GatumsGourmet.UnitTests --filter RefreshTokenTests`
→ falha de compilação esperada:
```
RefreshTokenTests.cs(12,25): error CS0246: O nome do tipo ou do namespace "RefreshToken" não pode ser encontrado
```
Neste momento a Application ainda compilava (o teste falhava só por faltar o tipo `RefreshToken`), confirmando a RED correta.

### GREEN (depois das entidades) — validação do Domain
`dotnet build src/GatumsGourmet.Domain`
```
GatumsGourmet.Domain -> ...\GatumsGourmet.Domain.dll
Compilação com êxito.
    0 Aviso(s)
    0 Erro(s)
```
O erro RED (`RefreshToken` não encontrado) deixou de existir.

### Impossibilidade de rodar o teste agora (concern esperado)
Após remover `Usuario.Role`, a compilação da **Application** quebra de propósito. Nova tentativa de `dotnet test ... --filter RefreshTokenTests` falha em:
`GetCurrentUserHandler`, `RegisterUserHandler`, `LoginHandler` (e demais consumidores de `usuario.Role`/`UserRole`).
Como o projeto de testes referencia Application, o teste `RefreshTokenTests` **não roda nesta task**. Conforme o brief, isso é esperado e será resolvido até a Task 7, quando a solução voltar a compilar. A evidência de GREEN das entidades é o build do Domain (0 erros) acima.

## Arquivos alterados
- A: `src/GatumsGourmet.Domain/Entities/Auth/Role.cs`
- A: `src/GatumsGourmet.Domain/Entities/Auth/Permission.cs`
- A: `src/GatumsGourmet.Domain/Entities/Auth/RolePermission.cs`
- A: `src/GatumsGourmet.Domain/Entities/Auth/UsuarioRole.cs`
- A: `src/GatumsGourmet.Domain/Entities/Auth/RefreshToken.cs`
- M: `src/GatumsGourmet.Domain/Entities/Auth/Usuario.cs`
- A: `tests/GatumsGourmet.UnitTests/Domain/Entities/Auth/RefreshTokenTests.cs`

## Auto-revisão (diff próprio)
- **Completude:** 5 entidades + modificação de `Usuario` + teste, todos presentes e verbatim ao brief.
- **Uma classe por arquivo:** respeitado nas 5 novas entidades e no teste.
- **Nomes claros / idioma:** nomenclatura em PT-BR consistente com o domínio (`Nome`, `Descricao`, `Recurso`, `Acao`, `FamiliaId`, `MotivoRevogacao`).
- **Padrão do repo:** namespace file-scoped, `= default!` para refs não-nulos, coleções inicializadas com `[]`, mesmo estilo de `Produto.cs`.
- **Encoding/BOM:** as entidades existentes (ex.: `Produto.cs`) não usam BOM; os novos arquivos seguem o mesmo (sem BOM) e contêm apenas ASCII, então não há acentuação a preservar. Consistência mantida.

## Concerns
1. **Teste não executável nesta task (esperado):** a quebra intencional da Application impede `dotnet test`. O `RefreshTokenTests` roda de fato na Task 7. Evidência de GREEN substituta: build do Domain com 0 erros.
2. **Consumidores quebrados (intencional, não corrigir aqui):** `LoginHandler`, `RegisterUserHandler`, `GetCurrentUserHandler`, `DbSeeder`, `UsuarioConfiguration` deixam de compilar por referenciarem `Usuario.Role`/`UserRole`. Serão corrigidos em tasks futuras.
