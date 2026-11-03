# Task 3: Quebrar InfrastructureServices; novo JwtTokenGenerator + RefreshTokenGenerator

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. Camada Infrastructure referencia Domain + Application.

**Objetivo:** (1) quebrar o arquivo monolítico `Services/InfrastructureServices.cs` em uma classe por arquivo; (2) reescrever o `JwtTokenGenerator` para a nova assinatura (claims de role e permissão, expiração de config); (3) criar o `RefreshTokenGenerator`.

## Constraints globais
- **Uma classe por arquivo.** Services de auth em `src/GatumsGourmet.Infrastructure/Services/Auth/`; mocks de pagamento em `src/GatumsGourmet.Infrastructure/Services/Pagamentos/`; `SystemClock` em `src/GatumsGourmet.Infrastructure/Services/SystemClock.cs`.
- net10.0, nullable enable. noBOM (padrão do repo). Use ferramentas de escrita, não echo/redirect.
- As interfaces da Task 2 já existem: `IJwtTokenGenerator.Generate(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissoes)`, `IRefreshTokenGenerator { (string PlainToken, string Hash) Generate(); string Hash(string plainToken); }`.

## Estado atual
`src/GatumsGourmet.Infrastructure/Services/InfrastructureServices.cs` contém HOJE 5 classes num arquivo só: `SystemClock`, `BcryptPasswordHasher`, `JwtTokenGenerator`, `MockPaymentGateway`, `MockPixProvider` (conteúdo completo abaixo). O registro está em `src/GatumsGourmet.Infrastructure/InfrastructureServiceCollectionExtensions.cs`.

Conteúdo atual do arquivo monolítico (para mover verbatim, exceto JwtTokenGenerator que muda):
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class SystemClock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
public class BcryptPasswordHasher : IPasswordHasher {
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
// JwtTokenGenerator(IConfiguration config) — assinatura ANTIGA (string role)
// MockPaymentGateway : IPaymentGateway — ProcessarAsync sempre aprova
// MockPixProvider : IPixProvider — GerarAsync + ConfirmarAsync (sempre true)
```

## Trabalho

### 1. Mover (sem alterar lógica), 1 classe por arquivo:
- `Services/SystemClock.cs` ← classe `SystemClock` (namespace `GatumsGourmet.Infrastructure.Services`).
- `Services/Auth/BcryptPasswordHasher.cs` ← classe `BcryptPasswordHasher` (namespace `GatumsGourmet.Infrastructure.Services.Auth`).
- `Services/Pagamentos/MockPaymentGateway.cs` ← `MockPaymentGateway` (namespace `GatumsGourmet.Infrastructure.Services.Pagamentos`).
- `Services/Pagamentos/MockPixProvider.cs` ← `MockPixProvider` (namespace `GatumsGourmet.Infrastructure.Services.Pagamentos`).
- **Apagar** `Services/InfrastructureServices.cs` ao final.

### 2. Novo `Services/Auth/JwtTokenGenerator.cs` (namespace `...Services.Auth`):
```csharp
public class JwtTokenGenerator(IConfiguration config) : IJwtTokenGenerator
{
    public string Generate(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissoes)
    {
        var secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var minutos = int.TryParse(config["Jwt:AccessTokenMinutes"], out var m) ? m : 15;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));
        foreach (var p in permissoes) claims.Add(new Claim("perm", p));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutos),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```
(usings necessários: System.IdentityModel.Tokens.Jwt, System.Security.Claims, System.Text, GatumsGourmet.Domain.Services.Interfaces, Microsoft.Extensions.Configuration, Microsoft.IdentityModel.Tokens)

### 3. Novo `Services/Auth/RefreshTokenGenerator.cs` (namespace `...Services.Auth`):
```csharp
public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public (string PlainToken, string Hash) Generate()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var plain = Base64UrlEncode(bytes);
        return (plain, Hash(plain));
    }

    public string Hash(string plainToken)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
```
(usings: System, System.Text)

### 4. Atualizar `InfrastructureServiceCollectionExtensions.cs`:
- Ajustar os `using` para os novos namespaces (`...Services.Auth`, `...Services.Pagamentos`, e manter `...Services` para SystemClock).
- Adicionar o registro: `services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();` junto dos outros services de auth.
- Manter todos os registros existentes (IJwtTokenGenerator → JwtTokenGenerator continua, agora com a assinatura nova).
- `IPermissionResolver` será registrado na Task 5 — NÃO registrar aqui (a impl ainda não existe).

## Testes (TDD) em `tests/GatumsGourmet.UnitTests/Infrastructure/Auth/`
- `JwtTokenGeneratorTests.cs` → `Token_contem_uma_claim_perm_por_permissao_e_role_por_role`:
  - Monta um `IConfiguration` em memória (`ConfigurationBuilder().AddInMemoryCollection(...)`) com `Jwt:Secret` (>=32 chars), `Jwt:Issuer`, `Jwt:Audience`, `Jwt:AccessTokenMinutes=15`.
  - Gera token com roles `["Admin","Gerente"]` e permissões `["produtos:escrever","pedidos:ler"]`.
  - Lê com `new JwtSecurityTokenHandler().ReadJwtToken(token)` e assevera: 2 claims `ClaimTypes.Role` (ou tipo "role" após mapeamento — use `token.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role")`), 2 claims com Type == "perm" com os valores certos, claim `sub` = userId, `email`.
- `RefreshTokenGeneratorTests.cs` → `Hash_e_estavel_e_plain_difere_do_hash`:
  - `Generate()` retorna PlainToken != Hash; `Hash(plain)` chamado 2x dá o mesmo valor; `Hash(plainToken)` == `Hash` retornado por `Generate()` para o mesmo plain.

Passos: escrever testes → rodar e ver falhar (`dotnet test tests/GatumsGourmet.UnitTests --filter "JwtTokenGeneratorTests|RefreshTokenGeneratorTests"`) → implementar → rodar e ver passar.

## IMPORTANTE sobre build
A Application continua quebrada de propósito (LoginHandler usa a assinatura antiga de IJwtTokenGenerator) — será corrigida na Task 7. Portanto o `dotnet test` do projeto de testes PODE falhar na compilação por causa da Application. Se isso acontecer: valide com `dotnet build src/GatumsGourmet.Infrastructure` (deve compilar 0 erros, pois Infra agora usa a assinatura nova) e reporte como concern que os testes rodam de fato na Task 7. Tente rodar os testes filtrados; se compilar, melhor.

## Commit
`refactor: separar services de infra e novo JwtTokenGenerator com claims de permissao`

## Não dispare subagentes. Não corrija LoginHandler (Task 7). Não registre IPermissionResolver (Task 5).
