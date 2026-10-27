# ADR 0002 — Refresh token rotativo no Postgres (Redis adiado)

- **Status:** Aceita
- **Data:** 2026-10-26
- **Contexto da feature:** Autenticação RBAC + refresh token (branch `feature/AjusteLoginToken`)

## Contexto

O modelo de autenticação anterior usava um **JWT único de 8h hardcoded**, sem
refresh token. O `logout` era no-op: não revogava nada, porque não havia estado
de sessão no servidor. Problemas:

- Token longo (8h) amplia a janela de uso indevido em caso de vazamento.
- Sem refresh, encurtar o token significaria obrigar relogin frequente.
- Sem revogação, "sair da conta" não tinha efeito real; trocar a senha não
  derrubava sessões existentes.

Era preciso um access token curto com renovação transparente e com capacidade de
revogação de sessão, sem introduzir infraestrutura nova neste momento.

## Decisão

Adotar **access token curto + refresh token opaco rotativo com detecção de reúso**,
com o store de refresh no **PostgreSQL**:

- **Access token**: JWT HS256, ~15 min (`Jwt:AccessTokenMinutes`), carregando as
  permissões efetivas (ver ADR 0001). Stateless.
- **Refresh token**: valor **opaco** aleatório (32 bytes, base64url). No banco
  grava-se apenas o **hash** (`RefreshTokens.TokenHash`), nunca o valor em claro.
  Validade ~7 dias (`Jwt:RefreshTokenDays`).
- **Rotação**: a cada `/auth/refresh` o token apresentado é revogado
  (`Revogado=true`, `SubstituidoPorTokenHash` → novo) e um refresh novo é emitido
  na **mesma família** (`FamiliaId`). O access também é reemitido, com permissões
  **recalculadas**.
- **Detecção de reúso (padrão OWASP)**: refresh tokens descendentes de um mesmo
  login compartilham `FamiliaId`. Se um refresh **já revogado** é reapresentado
  (sinal de roubo/replay), a **família inteira** é revogada e a operação falha,
  forçando relogin.
- **Logout real**: `/auth/logout` revoga a família do refresh informado;
  `/auth/logout-all` revoga todos os refresh ativos do usuário (útil após troca
  de senha). O access token em curso permanece válido até expirar (~15 min),
  comportamento esperado de JWT stateless.

O refresh trafega no **corpo da requisição** (SPA desacoplado), não em cookie
HttpOnly. O store é Postgres porque é o mesmo banco já usado em todos os
ambientes (Locaweb/Render/Neon) — nenhuma dependência de infra nova.

### Redis — avaliado e adiado

O Redis foi considerado para dois usos e **adiado** conscientemente:

1. **Denylist distribuída de access token** (revogação imediata do access, antes
   do fim dos 15 min).
2. **Store de refresh de baixa latência** sob alto volume.

Motivos para adiar: hoje a API roda em instância única e é usada apenas pelo
desenvolvedor; o Postgres atende com folga a leitura/escrita de refresh por login.
Introduzir Redis agora adiciona um componente de infra para operar, monitorar e
provisionar sem benefício prático imediato.

**Gatilhos para reavaliar (Fase 3):**
- Necessidade de **revogação imediata** de access tokens (denylist por `jti`).
- Operação em **múltiplas instâncias** onde estado compartilhado de baixa
  latência passe a importar.
- **Alto volume** de refresh que pressione o Postgres.

## Consequências

**Positivas**
- Janela de exposição de um access vazado cai de 8h para ~15 min.
- Renovação transparente via refresh, sem relogin frequente.
- Logout e "revogar todas as sessões" passam a ter efeito real.
- Detecção de reúso por família mitiga replay de refresh roubado.
- Refresh guardado só como hash: vazar a tabela não expõe tokens utilizáveis.
- Zero infraestrutura nova — só o Postgres já existente.

**Negativas / trade-offs**
- Access **não pode ser revogado imediatamente** — vale até expirar (~15 min).
  Aceitável dado o tempo de vida curto; revogação imediata fica para Fase 3.
- Estado de refresh no Postgres cresce; mitigado pelo housekeeping (ver ADR 0004).
- Sem retrocompatibilidade com o token antigo de 8h: a virada exige **relogin**.
  Aceitável porque o projeto é usado só pelo desenvolvedor e a janela é mínima
  (access 15 min + restart do processo).
