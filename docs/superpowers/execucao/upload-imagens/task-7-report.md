# Task 7 — relatório (documentação + Postman + verificação final)

Implementer: subagent fresco (general-task-execution). Verificado pelo orquestrador.

## Arquivos
- `docs/Site/Upload-Imagens.md` (novo) — espelha Busca-CEP.md: endpoint, auth produtos:escrever, campo `arquivo`, tipos por magic bytes, 5 MB, 201 `{ url }`, tabela de erros (400/401/403/500), consumo React+TanStack (uploadImagem → url → produto grava só fotoUrl), notas de storage por ambiente, nota técnica WHEN_REQUIRED, link API-Reference.
- `docs/Site/API-Reference.md` — seção `## Imagens` após Produtos (POST /imagens, multipart `arquivo`, 201 `{ url }`, 400/401/403/500) + linha na matriz de rotas `POST /imagens → produtos:escrever`.
- `README.md` — Stack corrigida (.NET 10, EF Core 10, RBAC+refresh, storage S3); tabela de endpoints completada (Categorias, Imagens, Endereços, Roles, Permissions, Usuários); nova seção `## Evolução` (V1, RBAC+refresh, CEP, upload imagens MinIO/Supabase/R2); nota do MinIO no docker compose (console 9001). Links verificados (docs/adr, docs/Auth, guias) — existem.
- `docs/Postman/GatumsGourmet.postman_collection.json` — pasta `Imagens` após Produtos com request "Upload de Imagem" (POST {{baseUrl}}/api/v1/imagens, bearer {{adminToken}}, body formdata campo `arquivo` type file, sem Content-Type manual, test 201 salvando fotoUrl) + variável de coleção `fotoUrl`. JSON validado (ConvertFrom-Json OK).

## Verificação final
- `dotnet build GatumsGourmet.sln` → 0 erros.
- Unit: 160 passed.
- Integração (suíte COMPLETA, verificada pelo orquestrador): **104/104 passed** (3m41s) — zero regressão em toda a API após as mudanças de Storage/DI.
- Integração UploadImagemEndpointTests: 5/5 (B real contra MinIO).
- Postman JSON válido.

## Desvios/minors
- Adicionada variável de coleção `fotoUrl` no Postman (coerência com o test script opcional). Sem impacto.
- Dívida de limpeza de imagens órfãs: mencionada de leve na doc, permanece fora de escopo (registrada na spec).
