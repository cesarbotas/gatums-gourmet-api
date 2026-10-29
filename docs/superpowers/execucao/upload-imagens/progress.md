# SDD ledger — plan: docs/superpowers/plans/2026-10-26-upload-imagens-s3.md

Spec: docs/superpowers/specs/2026-10-26-upload-imagens-s3-design.md
Execução: subagent-driven, branch feature/gravar-imagem-s3 (diretório principal).
BASE inicial (HEAD pré-task-1): c3875ef

Ruling: scripts bash da skill não rodam (WSL stub). Reproduzo brief/review-package com ferramentas nativas (fs_write + git diff via PowerShell). — mesmo ambiente da feature auth-rbac. — custo se errado: nenhum (fluxo lógico preservado).
Decisão do usuário: Task 5 = (A) teste com storage fake offline + (B) teste real contra MinIO quando o Docker estiver up (pulado se MinIO não responder na 9000).

## Pre-flight scan (conflitos entre tasks / auto-consistência)

| Área | Produz → Consome | Achado |
|------|------------------|--------|
| Task 1 → 2,3,5 | IImageStorage + ImageUploadResult + ImageTypeDetector | OK; assinatura UploadAsync(Stream,contentType,extensao,prefixo,ct) consistente em 2/3/5 |
| Task 2 → 4 | IUploadImagemHandler.Handle(stream,contentType,nome,tamanho,ct) + UploadImagemResponse(Url) | OK; endpoint da Task 4 consome exatamente isso |
| Task 3 → 4,5 | S3ImageStorage/UnconfiguredImageStorage + StorageOptions + DI | OK; fake da Task 5 implementa a mesma IImageStorage |
| Task 3 (config) | seção Storage em 5 appsettings | OK; Dev/LocalDocker→MinIO, Release→Supabase, Production→R2 |
| Task 4 → 5 | POST /api/v1/imagens multipart + RequirePermission(produtos:escrever) | OK; teste bate 201/403/401/400 |
| Task 6 | minio + minio-setup no docker-compose | independente; habilita o teste (B) da Task 5 |
| Task 7 | docs + Postman | depende de tudo; build+suíte final |
| auto T1 | teste WebP (RIFF/WEBP) vs texto | OK |
| auto T2 | magic bytes antes de tamanho? ordem: tamanho→tipo (brief) | OK |
| auto T5 | fake offline + real condicional (decisão B) | ajustar brief p/ incluir o teste real condicional |

Scan limpo. Nenhuma contradição que exija ruling antes da execução. A ordem Task 1→7 respeita dependências de interface.

## Tasks

(aguardando execução)

Task 1: dispatch (BASE c3875ef, implementer modelo econômico) — IImageStorage + ImageTypeDetector (magic bytes)

--- Rastro agora versionado em docs/superpowers/execucao/upload-imagens/ (commit 821eac2). ---
Task 1: re-dispatch (BASE 821eac2, implementer modelo econômico) — Task 1 anterior foi cancelada pelo usuário; nada implementado. Brief: task-1-brief.md.
Task 1: minor (deferred): ImageTypeDetector valida só o contêiner RIFF/WEBP, não sub-formatos VP8/VP8L/VP8X — suficiente p/ o escopo. Não bloqueante.
Task 1: complete (commits 821eac2..7546dd8, review clean). Build 0 erros; 5/5 testes do detector. Guardas de comprimento e cobertura WebP verificadas.

Task 2: dispatch (BASE 7546dd8, implementer modelo standard) — use case UploadImagem (validação tamanho + tipo)
Task 2: minor (deferred → Task 4): a validação de tamanho usa o parâmetro `tamanho` recebido, não o length real do stream. A Task 4 deve passar `IFormFile.Length` como fonte confiável. Não bloqueante no escopo do use case.
Task 2: complete (commits 7546dd8..5b3ae01, review clean — Approved, zero Critical/Important). Build 0 erros; 4/4 testes do handler. Reviewer confirmou: magic bytes como única fonte de verdade, MemoryStream rebobinado antes do upload, nenhum caminho de falha chama o storage (Times.Never).

Task 3: dispatch (BASE 5b3ae01, implementer modelo mais capaz — integração externa) — S3ImageStorage (AWSSDK.S3) + StorageOptions + UnconfiguredImageStorage + DI condicional + seção Storage nos 5 appsettings. REVIEW FOCUS: storage não configurado deve dar erro claro, não 500 opaco.
Task 3: ruling (desvio do brief) — o brief dizia `Services/Imagens/` mas a spec §8 e o plano (decisões já validadas) dizem `Services/Storage/`. Orquestrador moveu os 3 arquivos p/ `Services/Storage/`, ajustou namespace (`...Services.Storage`) + using no DI e rebuildou (0 erros). Prevalece a spec/plano; o brief estava desatualizado. — custo se errado: baixo (3 arquivos, rename mecânico).
Task 3: complete (working tree sobre 1bdd886, review Approved — zero Critical/Important). Build 0 erros (14 avisos pré-existentes). AWSSDK.S3 4.0.104.7; IAmazonS3 Singleton no DI; IImageStorage Scoped; DI condicional por `Storage:ServiceUrl`; UnconfiguredImageStorage falha explícita (não NRE). Seção Storage nos 4 appsettings (Dev/LocalDocker→MinIO, Release→Supabase, base→R2; segredos vazios p/ env var). Relatório: task-3-report.md; review: task-3-review.md.
Task 3: minor (deferred) — storage não configurado mapeia p/ HTTP 500 (mensagem clara no detail), em conformidade com spec §3; se quiser 4xx dedicado, rotear por DomainException no futuro. Compat real MinIO (checksum) validada só na Task 5/6.

Task 4: dispatch (BASE = commit da Task 3, implementer modelo standard) — endpoint POST /api/v1/imagens (multipart IFormFile) em Api/Endpoints/V1/Imagens/, RequirePermission("produtos:escrever"), DisableAntiforgery, passa ContentType/FileName/Length ao handler; mapear /imagens em EndpointRouteBuilderExtensions.
