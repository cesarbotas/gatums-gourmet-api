# Task 2 — Relatório: Use case UploadImagem (validação tamanho + tipo)

**Branch:** feature/gravar-imagem-s3
**BASE:** 796b46c (HEAD antes da task)
**Commit:** `5b3ae01` — `feat: use case UploadImagem com validacao de tamanho e tipo`

## O que foi criado

Use case seguindo o padrão do repo (Handler + Interface + Response, `Result<T>`,
primary constructor, uma classe por arquivo, noBOM):

- `src/GatumsGourmet.Application/UseCases/V1/Imagens/UploadImagem/UploadImagemResponse.cs`
  — `record UploadImagemResponse(string Url)`.
- `.../UploadImagem/IUploadImagemHandler.cs` — contrato
  `Task<Result<UploadImagemResponse>> Handle(Stream, string?, string?, long, CancellationToken)`.
  Doc deixa explícito que `contentType`/`nomeArquivo` são metadado não-confiável.
- `.../UploadImagem/UploadImagemHandler.cs` — dependências `IImageStorage storage,
  IConfiguration config`. Fluxo:
  1. `maxMb = config.GetValue<int?>("Storage:MaxFileSizeMB") ?? 5`.
  2. `tamanho <= 0` → `Fail("Arquivo vazio.")`.
  3. `tamanho > maxMb * 1024L * 1024L` → `Fail($"Arquivo excede o tamanho máximo de {maxMb} MB.")`.
  4. Copia `conteudo` → `MemoryStream` (uma vez), rebobina, lê até 12 bytes,
     `ImageTypeDetector.Detectar(...)`.
  5. `!ok` → `Fail("Tipo de imagem não suportado. Use JPEG, PNG ou WebP.")`.
  6. Rebobina o MS e chama `storage.UploadAsync(ms, ctype, extensao, "produtos", ct)`.
  7. `Ok(new UploadImagemResponse(resultado.Url))`.

## DI

`ApplicationServiceCollectionExtensions.AddApplication`: adicionado
`services.AddScoped<IUploadImagemHandler, UploadImagemHandler>();` + using do
namespace `...Imagens.UploadImagem`, logo após o registro do `BuscarEnderecoPorCep`.

## Evidência TDD

**RED** — testes escritos primeiro; `dotnet test --filter UploadImagemHandlerTests`
falhou na compilação (esperado, handler ainda não existia):
```
error CS0234: O nome de tipo ou namespace "UploadImagem" não existe no namespace
  "GatumsGourmet.Application.UseCases.V1.Imagens"
error CS0246: O nome do tipo ou do namespace "UploadImagemHandler" não pode ser encontrado
```

**GREEN** — após implementar + registrar no DI:
```
Resumo do teste: total: 4; falhou: 0; bem-sucedido: 4; ignorado: 0
```

## Build + Teste

- `dotnet build GatumsGourmet.sln` → **Construir êxito**, 0 erros (14 avisos
  pré-existentes: NU1902/NU1903 em IntegrationTests, MSB3277 EF Relational,
  CS0105 using duplicado em testes legados — nenhum introduzido por esta task).
- `dotnet test tests/GatumsGourmet.UnitTests --filter UploadImagemHandlerTests`
  → **4/4 verdes**.

## Testes (4)

1. `Handle_ImagemJpegValida_RetornaUrl` — bytes JPEG (FF D8 FF E0...), mock
   `UploadAsync(..., "image/jpeg", "jpg", "produtos", ...)` → URL correta +
   `UploadAsync` chamado 1×.
2. `Handle_Vazio_Falha` — `tamanho=0` → `Fail("Arquivo vazio.")`; `UploadAsync` nunca.
3. `Handle_AcimaDoLimite_Falha` — `tamanho=6MB` (stream pequeno) → `Fail` contém
   "tamanho máximo"; `UploadAsync` nunca.
4. `Handle_ConteudoNaoImagem_Falha` — bytes de texto + content-type "image/jpeg"
   mentido → `Fail("Tipo de imagem não suportado...")`; `UploadAsync` nunca
   (content-type mentido não engana a detecção por magic bytes).

## Arquivos alterados/criados

| Arquivo | Ação |
|---------|------|
| `src/.../Imagens/UploadImagem/UploadImagemResponse.cs` | criado |
| `src/.../Imagens/UploadImagem/IUploadImagemHandler.cs` | criado |
| `src/.../Imagens/UploadImagem/UploadImagemHandler.cs` | criado |
| `src/.../ApplicationServiceCollectionExtensions.cs` | editado (using + registro DI) |
| `tests/.../Imagens/UploadImagem/UploadImagemHandlerTests.cs` | criado |

5 files changed, 193 insertions(+).

## Auto-revisão

- noBOM verificado nos 4 arquivos criados (BOM=False em todos).
- Padrão do repo respeitado (comparado com `BuscarEnderecoPorCepHandler`): primary
  constructor, `Result<T>`, namespace file-scoped, doc XML.
- `using var ms` garante descarte do MemoryStream; o storage recebe o MS já
  rebobinado (Position=0), exatamente como o brief pede — resolve stream
  não-seekable e releitura.
- Leitura única `ms.ReadAsync(buffer, 0, 12)`: para `MemoryStream` isso retorna
  todos os bytes disponíveis até 12 num único read (não há risco de read parcial
  como em streams de rede), então não é necessário laço aqui.

## Concerns / dívidas

- **Validação de tamanho usa o parâmetro `tamanho` (declarado pelo cliente), não
  o comprimento real do stream.** É intencional conforme o brief (o teste 3 passa
  stream pequeno com `tamanho=6MB`). O endpoint (Task 4) deve repassar o tamanho
  real do `IFormFile.Length` e idealmente também confiar no limite de multipart do
  framework como defesa em profundidade. Não bloqueante.
- `contentType`/`nomeArquivo` ficam na assinatura mas não são usados na lógica
  (apenas metadado para o endpoint repassar) — conforme o brief.
- Herdado da Task 1: `ImageTypeDetector` valida só o contêiner RIFF/WEBP, não
  sub-formatos VP8/VP8L/VP8X. Fora do escopo desta task.
