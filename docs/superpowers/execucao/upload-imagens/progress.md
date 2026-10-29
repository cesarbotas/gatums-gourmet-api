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
Task 4: complete (working tree sobre c3f584f). Endpoint POST /api/v1/imagens (multipart IFormFile, [AsParameters] ImagensRequest), RequirePermission("produtos:escrever") + DisableAntiforgery, abre OpenReadStream e passa ContentType/FileName/**Length** ao handler → resolve o minor da Task 2. Registrado em EndpointRouteBuilderExtensions. Build 0 erros.
Task 4: ruling (ajuste pré-commit) — implementer usou `Results.BadRequest(new { erro })` na guarda de borda; orquestrador trocou por `Results.Problem(..., 400)` p/ manter o shape ProblemDetails dos demais erros 400 (ResultExtensions.ToHttpResult). Rebuild 0 erros. — custo se errado: baixo. Review self-feito (endpoint pequeno, 3 arquivos); concern único (shape de erro) resolvido. Relatório: task-4-report.md.

Task 5: dispatch (BASE = commit da Task 4, implementer modelo standard) — teste de integração: FakeImageStorage via ConfigureTestServices/RemoveAll (padrão BuscarEnderecoPorCepEndpointTests) — admin 201, cliente 403, anônimo 401, texto 400; + teste (B) real condicional contra MinIO (pulado se porta 9000 não responde).
Task 5: complete (working tree sobre 70ae696). Teste de integração UploadImagemEndpointTests: 5 passed (verificado pelo orquestrador, 117.7s). (A) fake offline: Admin→201+url+Chamado, Cliente→403, anônimo→401, texto→400+NãoChamado. (B) real contra MinIO: skip suave nesta máquina (9000 fechado) — nunca quebra a suíte. Auth+fake no mesmo host via WithWebHostBuilder + seed/login local. Só o arquivo de teste novo. Relatório: task-5-report.md.
Task 5: minor (deferred) — o caminho real do (B) contra MinIO só é exercido quando a Task 6 subir MinIO + bucket gatums-imagens; re-rodar após a Task 6.

Task 6: dispatch (BASE = commit da Task 5, implementer modelo econômico) — docker/docker-compose.yml: serviço minio (9000 API + 9001 console) + minio-setup (cria bucket gatums-imagens público). Credenciais minioadmin/minioadmin coerentes com appsettings Dev/LocalDocker.
Task 6: complete (working tree sobre 4248f42). docker/docker-compose.yml: serviço minio (9000 API + 9001 console) + bucket gatums-imagens público. Verificado end-to-end (container healthy, objeto gravado via teste B).
Task 6: ruling (imagem MinIO) — imagens oficiais MinIO removidas do Docker Hub (set/2026) e quay.io exige login (401 anônimo). Usei bitnamilegacy/minio pinada por digest; bucket criado via MINIO_DEFAULT_BUCKETS=gatums-imagens:public → removi o container minio-setup (dependia do minio/mc indisponível). — custo se errado: baixo (imagem dev-only; substituível).
Task 6: FIX DE PRODUÇÃO (bug da Task 3 exposto pelo MinIO real) — S3ImageStorage usava DisablePayloadSigning, que na V4 exige HTTPS e QUEBRARIA TODO upload em local/dev (http://). Removido; setado RequestChecksumCalculation=WHEN_REQUIRED no AmazonS3Config (desliga checksum CRC+aws-chunked que o MinIO rejeita, mantém SigV4 sobre http). Fonte: aws-sdk-net#3610. Arquivos: S3ImageStorage.cs + InfrastructureServiceCollectionExtensions.cs.
Task 6: correção do teste (B) da Task 5 — o teste antes mascarava erro de config como skip (AddInMemoryCollection perdia pro appsettings.json base R2). Reescrito p/ registrar S3ImageStorage real via ConfigureTestServices; agora tem teeth (skip SÓ se porta 9000 fechada). Foi ele quem revelou o bug acima.
Task 6: verificação — build 0 erros; unit 160 passed; integração UploadImagemEndpointTests 5/5 passed (inclui B real contra MinIO); objeto produtos/{guid}.jpg confirmado no bucket.

Task 7: dispatch (BASE = commit da Task 6, implementer modelo standard) — docs: README (evolução), docs/Site/Upload-Imagens.md (estilo Busca-CEP.md), API-Reference, Postman; build final + suíte completa.
Task 7: complete (working tree sobre f88a26b). Docs: Upload-Imagens.md (novo, estilo Busca-CEP), seção Imagens na API-Reference + matriz, README atualizado (.NET 10, RBAC+refresh, storage S3, tabela de endpoints, seção Evolução, nota MinIO), Postman (pasta Imagens + request multipart `arquivo` + var fotoUrl). JSON Postman válido. Relatório: task-7-report.md.
Task 7: verificação final — build 0 erros; unit 160 passed; integração COMPLETA 104/104 passed (zero regressão); UploadImagemEndpointTests 5/5. Postman JSON válido. Links do README conferidos (existem).

=== TODAS AS 7 TASKS CONCLUÍDAS. Falta: review final da branch inteira + finishing-a-development-branch (usuário decide merge/PR; ele disse que faz PR manual). ===
