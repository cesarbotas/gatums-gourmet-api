# ADR 0004 — Limpeza (housekeeping) de refresh tokens

- **Status:** Aceita
- **Data:** 2026-10-26
- **Contexto da feature:** Autenticação RBAC + refresh token (branch `feature/AjusteLoginToken`)

## Contexto

Com o refresh token rotativo (ADR 0002), cada login e cada renovação gera uma
linha em `RefreshTokens`. Com rotação, tokens antigos ficam marcados como
revogados mas **não somem** — a tabela só cresce. Tokens expirados ou revogados
há muito tempo não têm mais utilidade (expirado não autentica; a detecção de
reúso só precisa da família enquanto ela ainda pode ser reapresentada dentro da
validade). Sem limpeza, a tabela acumula lixo indefinidamente.

Era preciso uma rotina de retenção que apague tokens fora da janela útil, sem
introduzir um agendador externo neste momento.

## Decisão

Implementar o housekeeping com um **caso de uso único** reutilizado por dois
gatilhos:

- **Caso de uso**: `UseCases/V1/Manutencao/PurgeExpiredRefreshTokens/`, que apaga
  tokens fora da retenção via `IRefreshTokenStore.DeleteExpiredAsync(cutoff, ct)`.
- **L1 — BackgroundService** (`RefreshTokenCleanupService` na Api): roda em
  intervalo configurável e executa o caso de uso automaticamente.
- **L3 — Endpoint manual** (`POST /api/v1/auth/maintenance/purge-tokens`,
  protegido por `usuarios:gerenciar`): gatilho sob demanda, chama o **mesmo** caso
  de uso. Útil para rodar a limpeza manualmente ou validar o comportamento.

**Configuração** (`appsettings`):
- `Auth:RefreshTokenRetentionDays` (padrão **30**) — retenção além da expiração.
- `Auth:CleanupIntervalHours` (padrão **12**) — frequência do BackgroundService.

**Evolução futura (registrada):** um agendador externo estilo Control-M poderia
orquestrar a rotina em vez do BackgroundService interno, útil se a manutenção
precisar ser coordenada com outras rotinas ou movida para fora do processo da API.

## Consequências

**Positivas**
- A tabela `RefreshTokens` não cresce sem limite; lixo expirado/revogado é
  removido automaticamente.
- Um único caso de uso para os dois gatilhos (automático e manual) — sem
  duplicação de lógica.
- Parâmetros de retenção e frequência ajustáveis por ambiente, sem deploy de
  código.
- O endpoint manual dá um gatilho explícito para operação e testes.

**Negativas / trade-offs**
- O BackgroundService roda **dentro do processo da API**: em múltiplas instâncias,
  cada uma rodaria a limpeza (idempotente, mas com trabalho redundante). Um
  agendador externo resolveria isso — fica para a evolução futura.
- A retenção de 30 dias é uma heurística; muito curta pode apagar famílias ainda
  relevantes para auditoria, muito longa mantém lixo por mais tempo. É
  configurável justamente para ajuste por ambiente.
