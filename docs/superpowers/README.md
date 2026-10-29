# Memória do projeto (specs, planos e execução)

Esta pasta é a **memória versionada** do projeto Gatums Gourmet: tudo que foi
discutido, validado e executado fica aqui, no Git. Ao abrir o projeto em **outra
máquina ou com outro usuário**, leia este índice primeiro e retome de onde parou,
mantendo o padrão — sem depender do que estava só numa máquina ou na memória de
uma conversa.

> **Como retomar o projeto (leia primeiro):**
> 1. Leia este README e a tabela de features abaixo.
> 2. Para a feature em andamento, abra `execucao/<feature>/progress.md` (o ledger):
>    ele diz quais tarefas estão `complete`, qual está no meio, e registra todas as
>    decisões (`Ruling:`), dívidas diferidas e pointers entre tarefas.
> 3. Confirme o estado real contra o `git log` (os commits são a fonte da verdade do
>    código; o ledger é a fonte da verdade do processo/decisões).
> 4. Leia a `spec` e o `plan` da feature antes de escrever qualquer código.

## Estrutura

| Pasta | O que é | Versionado |
|-------|---------|------------|
| `specs/` | SDDs — design validado de cada feature (o "o quê" e "por quê") | sim |
| `plans/` | Planos de implementação (as tarefas, a ordem, os testes) | sim |
| `execucao/<feature>/` | Rastro da execução: `progress.md` (ledger), `task-N-brief.md`, `task-N-report.md` | sim |
| *(review packages)* | Dumps de `git diff` para os revisores — reconstituíveis a partir dos commits | **não** (git-ignored) |

## Features

| Feature | Spec | Plano | Execução | Status |
|---------|------|-------|----------|--------|
| Autenticação RBAC + refresh token | `specs/2026-10-21-auth-rbac-refresh-token-design.md` | `plans/2026-10-21-auth-rbac-refresh-token.md` | `execucao/auth-rbac/` | Concluída (16 tasks + fix-wave; branch `feature/AjusteLoginToken`) |
| Busca de CEP (ViaCEP) | — (bounded) | — | — | Concluída (branch `feature/BuscaCep`) |
| Upload de imagens (S3: MinIO/Supabase/R2) | `specs/2026-10-26-upload-imagens-s3-design.md` | `plans/2026-10-26-upload-imagens-s3.md` | `execucao/upload-imagens/` | Concluída (7 tasks + review final; PR #35 no `develop`) |
| Padronização endpoints (`[AsParameters]`) | — (mecânica) | — | `execucao/padronizar-endpoints/` | Concluída (branch `feature/padronizar-endpoints-asparameters`) |

## Como o rastro é gravado

Durante a execução subagent-driven, cada tarefa gera:
- uma entrada no `progress.md` (dispatch → complete, com os commits e findings),
- um `task-N-brief.md` (o que foi pedido à tarefa),
- um `task-N-report.md` (o que o implementer fez + evidência de teste).

Esses arquivos são commitados junto com a tarefa, então o histórico de processo
viaja com o repositório. Os review packages (diffs grandes) ficam fora do Git
porque o mesmo diff já está nos commits.

## Nota sobre as skills do Superpowers

A **metodologia** (brainstorming → writing-plans → subagent-driven-development →
finishing) vem de skills instaladas no ambiente de quem desenvolve
(`~/.kiro/skills/`), **não** no repositório. Para seguir o mesmo fluxo em outra
máquina, instale o Superpowers nela. O que o repositório garante é a **memória**
(esta pasta) e o **código** (Git) — qualquer pessoa retoma o projeto por aqui,
com ou sem as skills instaladas.
