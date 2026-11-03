# Task 5 — relatório de implementação (testes de integração do upload)

Implementer: subagent fresco (general-task-execution). `dotnet test` (filtro UploadImagemEndpointTests): 5 passed, 0 failed. Verificado também pelo orquestrador (5 passed, 117.7s).

## Arquivo criado
`tests/GatumsGourmet.IntegrationTests/Application/UseCases/V1/Imagens/UploadImagemEndpointTests.cs` (sem BOM, UTF-8).

## Auth + fake no mesmo host
`CreateAuthenticatedClientAsync` do projeto usa `factory.CreateClient()` (host sem fake). Para combinar auth real + storage fake na MESMA requisição, o teste:
- `HostComFake(fake)` = `_factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => { s.RemoveAll<IImageStorage>(); s.AddScoped<IImageStorage>(_ => fake); }))`.
- função local `ClientAutenticadoAsync(host, role)` replica o mínimo do AuthenticatedClientFactory sobre ESSE host: semeia Usuario+Role+UsuarioRole, login real em /api/v1/auth/login, devolve `host.CreateClient()` com Bearer.

## Casos (A) — offline com FakeImageStorage (todos pass)
- Admin + JPEG → 201, url não-vazia, `fake.Chamado == true`.
- Cliente + JPEG → 403 (sem produtos:escrever).
- Anônimo + JPEG → 401.
- Admin + texto → 400 (magic bytes), `fake.Chamado == false`.
Multipart: `MultipartFormDataContent` + `ByteArrayContent`, campo "arquivo", JPEG `FF D8 FF E0 ...`.

## Caso (B) — real condicional contra MinIO
Probe TCP localhost:9000. Nesta máquina MinIO NÃO está up → **skip suave** (early-return, log "MinIO indisponível, teste (B) pulado" via ITestOutputHelper). Se 9000 responder: host com seção Storage→MinIO + S3ImageStorage real; PUT envolto em try + checagem status != 201 → skip suave. Nunca quebra a suíte por ambiente ausente.

## Desvios/decisões
- Skip do (B) por early-return (xUnit v2 não tem Skip dinâmico fácil) — alinhado ao brief; aparece como "passed" no runner.
- `ITestOutputHelper` no construtor p/ log visível do skip (observabilidade; sem mudar comportamento).

## Dívidas deferidas
- O caminho real do (B) contra MinIO só será exercido quando a Task 6 subir MinIO + bucket `gatums-imagens`. Re-rodar após a Task 6 para validar end-to-end.
