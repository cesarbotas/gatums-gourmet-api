# Contexto do projeto — ler antes de agir

Este projeto mantém sua memória versionada em `docs/superpowers/`. Antes de
planejar ou implementar qualquer mudança, consulte esse contexto já validado —
isso garante continuidade entre máquinas e usuários e mantém o padrão do projeto.

## No início de uma tarefa de desenvolvimento

1. Leia `docs/superpowers/README.md` (índice de features: specs, planos, execução).
2. Se a tarefa continua uma feature em andamento, leia o ledger dela em
   `docs/superpowers/execucao/<feature>/progress.md` — ele registra o estado das
   tarefas, as decisões tomadas (`Ruling:`), dívidas diferidas e pointers entre
   tarefas. Confirme o estado contra o `git log`.
3. Leia a `spec` (`docs/superpowers/specs/`) e o `plan` (`docs/superpowers/plans/`)
   da feature relevante antes de escrever código.
4. Não refaça decisões já validadas nesses documentos; se precisar divergir,
   registre o motivo.

## Padrões do repositório (resumo)

- Clean Architecture (.NET 10): Domain / Application / Infrastructure / Api.
- Uma classe por arquivo; use case = Handler + Interface + Request/Response, com `Result<T>`.
- Endpoints Minimal API versionados em `Api/Endpoints/V1`; autorização por permissão
  (`RequirePermission("recurso:acao")`).
- Integrações externas são abstraídas por interface no Domain e implementadas na
  Infrastructure (ex.: `ICepProvider`, `IImageStorage`), configuradas por ambiente.
- Testes: xUnit + Moq + FluentAssertions (unit) e WebApplicationFactory (integração).
- Arquivos `.cs` sem BOM; preservar acentuação (UTF-8).

## Rastro de execução

Ao executar um plano via subagent-driven-development, grave o ledger e os
relatórios de cada tarefa em `docs/superpowers/execucao/<feature>/` (versionado).
Apenas os review packages (dumps de diff) ficam fora do Git.
