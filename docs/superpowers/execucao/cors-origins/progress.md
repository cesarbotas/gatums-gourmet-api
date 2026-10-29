# CORS por origem (restrição por ambiente)

Branch `feature/cors-origins` (a partir de `develop` pós-fix de paginação). Bounded — mudança no CORS que já existia no Program.cs.

## Design aprovado pelo usuário
- Development/LocalDocker: `AllowAnyOrigin` (conveniência local).
- HML (Release): padrão curinga `https://gatums-gourmet*.vercel.app` (cobre o fixo `gatums-gourmet-hub.vercel.app` e os previews `gatums-gourmet-<hash>-...vercel.app`).
- PRD (Production): origem exata `https://app.gatumsgourmet.com.br`.
- Extension `CorsExtensions` (`AddGatumsCors`/`UseGatumsCors`) lendo `Cors:AllowedOrigins` (exatas) + `Cors:AllowedOriginPatterns` (curingas), com validação fail-fast no startup e normalização (sem barra final/path). Sem `AllowCredentials` (auth é Bearer, não cookie).

## Decisões técnicas
- **Leitura lazy da config via `IConfigureOptions<CorsOptions>`** (resolvido do DI). Motivo (systematic-debugging): ler a config no momento do registro (`AddGatumsCors(builder.Configuration)`) NÃO enxergava a config injetada por `ConfigureAppConfiguration` nos testes — o diagnóstico mostrou `origins=[] patterns=[]` em Test. Resolver do DII pega a `IConfiguration` final (todas as fontes mescladas). Também vale em produção.
- **Match por `SetIsOriginAllowed`** cobrindo origens exatas + regex dos curingas numa única avaliação (não combina com `WithOrigins`, para evitar ambiguidade).
- **Curinga → regex**: `*` vira `[^/]*` (não atravessa a barra de path), âncorado `^...$`. Assim `gatums-gourmet*.vercel.app` NÃO vira "qualquer vercel.app" (teste cobre `outro-projeto.vercel.app` → negado).
- **Normalização**: remove barra final e espaços; descarta origens/padrões vazios. Barra final é tolerada; path/query → inválido (fail-fast).
- **Fail-fast real no startup**: `UseGatumsCors` força `GetRequiredService<IOptions<CorsOptions>>().Value` no setup do pipeline, então a validação roda no boot (não na 1ª requisição). Em Release/PRD sem origem/padrão → host não sobe.
- **Ambiente Test**: não-Development → ramo restritivo; com lista vazia NÃO falha (só Release/Production falham), nega tudo silenciosamente. Mantém os testes existentes (que não mandam header Origin) passando.

## Config (appsettings)
- `appsettings.json` (base/PRD): `Cors:AllowedOrigins = ["https://app.gatumsgourmet.com.br"]`.
- `appsettings.Release.json` (HML): `Cors:AllowedOriginPatterns = ["https://gatums-gourmet*.vercel.app"]`, `AllowedOrigins = []`.
- Dev/LocalDocker: sem seção Cors (cai no AllowAnyOrigin).
- Ajustável por env var `Cors__AllowedOrigins__0` / `Cors__AllowedOriginPatterns__0`.

## TDD
- RED: `tests/.../Api/Cors/CorsPolicyTests.cs` — origem exata PRD, curinga HML (fixo + preview), origem não listada, vercel de outro projeto, fail-fast sem config, path inválido, barra final tolerada. Contra o `AllowAnyOrigin` atual: 4/5 falhavam.
- GREEN: extension + wiring. 8/8 passam.

## Verificação
- Build solution: 0 erros. Unit: 160/160. Integração: **116/116** (108 + 8 CORS).

## Status
Concluído. Pronto para PR no develop.
