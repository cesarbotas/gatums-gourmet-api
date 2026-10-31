# Task 3 — Report: Quebrar InfrastructureServices + novo JwtTokenGenerator + RefreshTokenGenerator

**Branch:** feature/AjusteLoginToken
**Data:** execução da Task 3

## O que foi movido (1 classe por arquivo, sem mudar lógica)
- `Services/SystemClock.cs` ← `SystemClock` (ns `GatumsGourmet.Infrastructure.Services`)
- `Services/Auth/BcryptPasswordHasher.cs` ← `BcryptPasswordHasher` (ns `...Services.Auth`)
- `Services/Pagamentos/MockPaymentGateway.cs` ← `MockPaymentGateway` (ns `...Services.Pagamentos`)
- `Services/Pagamentos/MockPixProvider.cs` ← `MockPixProvider` (ns `...Services.Pagamentos`)
- **Apagado:** `Services/InfrastructureServices.cs` (monolítico de 5 classes)

## O que foi criado (verbatim ao brief)
- `Services/Auth/JwtTokenGenerator.cs` — nova assinatura `Generate(Guid, string email, IEnumerable<string> roles, IEnumerable<string> permissoes)`.
  Claims: `sub`, `email`, `jti`, uma `ClaimTypes.Role` por role, uma `"perm"` por permissão. Expiração = `Jwt:AccessTokenMinutes` (default 15).
- `Services/Auth/RefreshTokenGenerator.cs` — `Generate()` = 32 bytes aleatórios (RandomNumberGenerator) em base64url + hash SHA-256 hex (lowercase); `Hash(plain)` estável.

## DI atualizado
`InfrastructureServiceCollectionExtensions.cs`:
- Novos usings: `...Services.Auth`, `...Services.Pagamentos` (mantido `...Services` para SystemClock).
- Adicionado `services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();`.
- `IJwtTokenGenerator → JwtTokenGenerator` mantido (agora com assinatura nova).
- `IPermissionResolver` NÃO registrado (Task 5).

## Testes (TDD)
- `tests/GatumsGourmet.UnitTests/Infrastructure/Auth/JwtTokenGeneratorTests.cs` → `Token_contem_uma_claim_perm_por_permissao_e_role_por_role`.
- `tests/GatumsGourmet.UnitTests/Infrastructure/Auth/RefreshTokenGeneratorTests.cs` → `Hash_e_estavel_e_plain_difere_do_hash`.
- Adicionada ProjectReference de Infrastructure no `GatumsGourmet.UnitTests.csproj` (os testes precisam enxergar as classes concretas).

### Evidência RED/GREEN
- **RED:** antes de criar as classes, `dotnet test --filter "JwtTokenGeneratorTests|RefreshTokenGeneratorTests"` falhou na compilação. Erros mostrados eram do projeto **Application** (`Usuario.Role` — LoginHandler/RegisterUserHandler/GetCurrentUserHandler), que está quebrado de propósito (Task 7). Isso impede a compilação do projeto de testes.
- **GREEN (parcial, conforme brief):** `dotnet build src/GatumsGourmet.Infrastructure` → os únicos 4 erros são do projeto Application (transitivo). Filtrando a saída por arquivos de Infrastructure: **0 erros** originados em Infrastructure. O código novo de Infra compila; o build transitivo falha só por causa da Application.

## Resultado build/test
- `dotnet build src/GatumsGourmet.Infrastructure`: 0 erros em arquivos de Infrastructure; 4 erros transitivos em Application (esperado, Task 7).
- `dotnet test` filtrado não executa os 2 testes ainda porque o projeto de testes referencia Application (não compila). Serão executáveis após a Task 7 corrigir `Usuario.Role`/LoginHandler.

## Arquivos alterados/criados
- A: `src/GatumsGourmet.Infrastructure/Services/SystemClock.cs`
- A: `src/GatumsGourmet.Infrastructure/Services/Auth/BcryptPasswordHasher.cs`
- A: `src/GatumsGourmet.Infrastructure/Services/Auth/JwtTokenGenerator.cs`
- A: `src/GatumsGourmet.Infrastructure/Services/Auth/RefreshTokenGenerator.cs`
- A: `src/GatumsGourmet.Infrastructure/Services/Pagamentos/MockPaymentGateway.cs`
- A: `src/GatumsGourmet.Infrastructure/Services/Pagamentos/MockPixProvider.cs`
- D: `src/GatumsGourmet.Infrastructure/Services/InfrastructureServices.cs`
- M: `src/GatumsGourmet.Infrastructure/InfrastructureServiceCollectionExtensions.cs`
- M: `tests/GatumsGourmet.UnitTests/GatumsGourmet.UnitTests.csproj`
- A: `tests/GatumsGourmet.UnitTests/Infrastructure/Auth/JwtTokenGeneratorTests.cs`
- A: `tests/GatumsGourmet.UnitTests/Infrastructure/Auth/RefreshTokenGeneratorTests.cs`

## Auto-revisão
- Lógica dos mocks e do BcryptPasswordHasher movida verbatim (sem alterações). SystemClock idem.
- JwtTokenGenerator e RefreshTokenGenerator escritos exatamente como no brief.
- DI: apenas um registro adicionado (RefreshTokenGenerator); nenhum registro existente removido; IPermissionResolver não tocado.
- Namespaces conferem com os caminhos de pasta. Comentário com caractere corrompido (`â€”`) foi preservado verbatim do original para não alterar lógica/conteúdo; pode ser normalizado depois se desejado.
- LoginHandler NÃO foi tocado (Task 7).

## Concerns
1. **Testes não rodam de fato nesta task:** o projeto Application está quebrado de propósito (Task 7). Como o projeto de testes referencia Application, ele não compila e os 2 testes novos não executam. Validação feita via build de Infrastructure (0 erros em arquivos de Infra). Os testes devem passar assim que a Task 7 corrigir a Application.
2. **ProjectReference de Infrastructure adicionada ao projeto de testes** — necessária para testar as implementações concretas; não estava prevista explicitamente no brief mas é pré-requisito para os testes.
3. Comentários legados contêm um caractere mojibake (`â€”`) herdado do arquivo original; mantido para preservar conteúdo verbatim.

## Commit
- `c582eaf` — refactor: separar services de infra e novo JwtTokenGenerator com claims de permissao (11 arquivos; report fica fora do git pois `.superpowers` é gitignored)
