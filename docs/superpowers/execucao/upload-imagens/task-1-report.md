# Task 1 — Relatório: Contrato IImageStorage + detecção de tipo por magic bytes

**Status:** DONE
**Branch:** feature/gravar-imagem-s3
**Commit:** `7546dd8` — feat: contrato IImageStorage e deteccao de tipo por magic bytes

## O que foi criado

1. `src/GatumsGourmet.Domain/Services/Interfaces/IImageStorage.cs`
   - `record ImageUploadResult(string Url, string ObjectKey)` + `interface IImageStorage` com
     `Task<ImageUploadResult> UploadAsync(Stream, string contentType, string extensao, string prefixo, CancellationToken = default)`.
   - Record + interface no mesmo arquivo (tipos pequenos relacionados), seguindo o padrão do repo
     (ex.: `ICepProvider.cs` que agrupa `interface` + `record EnderecoCep`).
2. `src/GatumsGourmet.Application/UseCases/V1/Imagens/ImageTypeDetector.cs`
   - Classe estática com `Detectar(ReadOnlySpan<byte> prefixo)` retornando `(bool Ok, string Extensao, string ContentType)`.
   - JPEG (`FF D8 FF`, ≥3 bytes) → ("jpg","image/jpeg"); PNG (`89 50 4E 47 0D 0A 1A 0A`, ≥8 bytes) →
     ("png","image/png"); WebP (`RIFF`...`WEBP`, ≥12 bytes, ignorando os bytes 4-7 de tamanho) →
     ("webp","image/webp"); caso contrário `(false, "", "")`.
3. `tests/GatumsGourmet.UnitTests/Application/UseCases/V1/Imagens/ImageTypeDetectorTests.cs`
   - `[Theory]` + `[MemberData]` para os 3 casos reconhecidos (JPEG, PNG, WebP) e `[Fact]` para
     texto "Hello" e buffer vazio. xUnit + FluentAssertions.

## Evidência TDD

- **RED:** com o teste escrito e sem a implementação, `dotnet test --filter ImageTypeDetectorTests`
  falhou na compilação:
  `error CS0234: O nome de tipo ou namespace "Imagens" não existe no namespace
  "GatumsGourmet.Application.UseCases.V1"`.
- **GREEN:** após criar os dois arquivos de produção, o teste filtrado passou:
  `Aprovado! – Com falha: 0, Aprovado: 5, Ignorado: 0, Total: 5` (net10.0).

## Build + teste

- `dotnet build GatumsGourmet.sln` → **Compilação com êxito** (0 erros). Sem bloqueio de DLL
  (processo GatumsGourmet.Api não estava rodando / não houve MSB3021/MSB3027).
- `dotnet test tests/GatumsGourmet.UnitTests --filter ImageTypeDetectorTests` → **5/5 verdes**.

## Arquivos alterados (commit 7546dd8)

- `src/GatumsGourmet.Domain/Services/Interfaces/IImageStorage.cs` (novo)
- `src/GatumsGourmet.Application/UseCases/V1/Imagens/ImageTypeDetector.cs` (novo)
- `tests/GatumsGourmet.UnitTests/Application/UseCases/V1/Imagens/ImageTypeDetectorTests.cs` (novo)

(`docs/superpowers/execucao/upload-imagens/progress.md` ficou fora deste commit — é memória de
execução, não código da task.)

## Auto-revisão

- Clean Architecture respeitada: contrato no Domain (sem dependências), detector na Application.
- Uma responsabilidade por arquivo; nomes e documentação XML no padrão do repo.
- WebP (review focus): valida `RIFF` (bytes 0-3) **e** `WEBP` (bytes 8-11), ignorando os bytes 4-7
  (campo de tamanho do RIFF) — por isso o teste usa `0,0,0,0` nessa região e passa. Exige ≥12 bytes.
- Guardas de comprimento (`>= 3/8/12`) garantem que buffers curtos caem no caminho `(false, "", "")`
  sem acesso fora de faixa — coberto pelo teste de buffer vazio.
- Arquivos `.cs` criados via ferramentas de escrita (sem BOM). Git avisou LF→CRLF (normalização de
  checkout no Windows, comportamento padrão do repo), sem impacto no build/teste.

## Concerns

- Nenhum bloqueante. A validação de tipo por magic bytes não cobre todos os sub-formatos de WebP
  (VP8/VP8L/VP8X), mas o contêiner RIFF/WEBP é suficiente para o escopo desta feature; refinamentos
  ficam para tasks de validação de upload, se necessário.
