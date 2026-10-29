# Task 2: Use case UploadImagem (validação de tamanho + tipo)

**Projeto:** API .NET 10 (Clean Architecture), branch feature/gravar-imagem-s3, em c:\Git\Botas\gatums-gourmet-api.

**Objetivo:** o caso de uso que valida o arquivo (tamanho + tipo por magic bytes) e delega a gravação ao `IImageStorage`.

## Constraints globais
- net10.0, noBOM, uma classe por arquivo. Padrão de use case do repo: Handler + Interface + Response no(s) arquivo(s) da pasta do use case, com `Result<T>`. Ver `BuscarEnderecoPorCepHandler` como referência de estilo (validação + chamada a provider externo + Result).
- DI manual em `ApplicationServiceCollectionExtensions` (registrar o handler).

## Disponível (Task 1)
- `GatumsGourmet.Domain.Services.Interfaces`: `IImageStorage.UploadAsync(Stream conteudo, string contentType, string extensao, string prefixo, CancellationToken ct = default) → Task<ImageUploadResult(string Url, string ObjectKey)>`.
- `GatumsGourmet.Application.UseCases.V1.Imagens.ImageTypeDetector.Detectar(ReadOnlySpan<byte>) → (bool Ok, string Extensao, string ContentType)`.
- `GatumsGourmet.Domain.Common.V1.Result<T>` (Ok/Fail).

## Arquivos

### `Application/UseCases/V1/Imagens/UploadImagem/UploadImagemResponse.cs`
```csharp
namespace GatumsGourmet.Application.UseCases.V1.Imagens.UploadImagem;
public record UploadImagemResponse(string Url);
```

### `.../UploadImagem/IUploadImagemHandler.cs`
```csharp
using GatumsGourmet.Domain.Common.V1;
namespace GatumsGourmet.Application.UseCases.V1.Imagens.UploadImagem;
public interface IUploadImagemHandler
{
    Task<Result<UploadImagemResponse>> Handle(Stream conteudo, string? contentType, string? nomeArquivo, long tamanho, CancellationToken ct = default);
}
```
(`contentType`/`nomeArquivo` são do cliente e NÃO são confiáveis para decidir o tipo — servem só como metadado; o tipo real vem dos magic bytes. Podem nem ser usados na lógica — ok deixá-los na assinatura para o endpoint repassar.)

### `.../UploadImagem/UploadImagemHandler.cs`
Dependências (primary constructor): `IImageStorage storage, IConfiguration config`.
Fluxo do `Handle`:
1. `maxMb = config.GetValue<int?>("Storage:MaxFileSizeMB") ?? 5;`
2. Se `tamanho <= 0` → `Result<UploadImagemResponse>.Fail("Arquivo vazio.")`.
3. Se `tamanho > maxMb * 1024L * 1024L` → `Fail($"Arquivo excede o tamanho máximo de {maxMb} MB.")`.
4. Lê os primeiros bytes para detecção. O stream pode não ser totalmente carregado; leia até 12 bytes num buffer:
   ```
   var buffer = new byte[12];
   var lidos = await ReadAtLeastAsync(conteudo, buffer, ct); // ou conteudo.ReadAsync em loop até encher ou EOF
   var (ok, ext, ctype) = ImageTypeDetector.Detectar(buffer.AsSpan(0, lidos));
   ```
   Use `conteudo.ReadAsync` num laço simples para preencher o buffer (streams podem retornar menos bytes por vez). Se o stream não for seekable, bufferize o conteúdo inteiro num MemoryStream ANTES (o IFormFile do ASP.NET é seekable, mas o handler não deve assumir — para robustez e para reler do início, copie para um MemoryStream: `var ms = new MemoryStream(); await conteudo.CopyToAsync(ms, ct); ms.Position = 0;` e trabalhe com `ms`).
5. Se `!ok` → `Fail("Tipo de imagem não suportado. Use JPEG, PNG ou WebP.")`.
6. Rebobina o stream de upload (`ms.Position = 0`) e chama `storage.UploadAsync(ms, ctype, ext, "produtos", ct)`.
7. Retorna `Result<UploadImagemResponse>.Ok(new UploadImagemResponse(result.Url))`.

Recomendação de implementação: no passo 4, copie `conteudo` para um `MemoryStream` uma vez, detecte pelos primeiros bytes desse MS, rebobine e passe o MS ao storage. Isso evita problemas de stream não-seekable e de reler.

### DI
Registrar em `ApplicationServiceCollectionExtensions.AddApplication`: `services.AddScoped<IUploadImagemHandler, UploadImagemHandler>();` (+ using do namespace).

## Testes (TDD) — `tests/GatumsGourmet.UnitTests/Application/UseCases/V1/Imagens/UploadImagem/UploadImagemHandlerTests.cs`
Mock `IImageStorage` + `IConfiguration` InMemory (`Storage:MaxFileSizeMB=5`). Helper para montar um `MemoryStream` com bytes dados.
1. `Handle_ImagemJpegValida_RetornaUrl`: stream com bytes JPEG (`FF D8 FF E0` + alguns bytes), `tamanho` = length do stream. Mock `storage.UploadAsync(It.IsAny<Stream>(), "image/jpeg", "jpg", "produtos", It.IsAny<CancellationToken>())` retorna `new ImageUploadResult("https://x/produtos/a.jpg","produtos/a.jpg")`. Assert: `IsSuccess`, `Data.Url == "https://x/produtos/a.jpg"`, e `UploadAsync` chamado 1×.
2. `Handle_Vazio_Falha`: `tamanho = 0` → Fail "Arquivo vazio."; `UploadAsync` nunca (REVIEW FOCUS parcial).
3. `Handle_AcimaDoLimite_Falha`: `tamanho = 6*1024*1024` (stream pode ser pequeno; o que importa é o parâmetro tamanho) → Fail contendo "tamanho máximo"; `UploadAsync` nunca (REVIEW FOCUS).
4. `Handle_ConteudoNaoImagem_Falha`: stream com bytes de texto ("Hello..."), tamanho>0 → Fail "Tipo de imagem não suportado. Use JPEG, PNG ou WebP."; `UploadAsync` nunca (REVIEW FOCUS — content-type mentido não engana).

Siga TDD: escreva os testes, veja falhar, implemente, veja passar.

## Validação
`dotnet build GatumsGourmet.sln` 0 erros; `dotnet test tests/GatumsGourmet.UnitTests --filter UploadImagemHandlerTests` verde. (Se a API estiver rodando e travar DLLs, encerre o processo GatumsGourmet.Api.)

## Commit
`feat: use case UploadImagem com validacao de tamanho e tipo`

## Não dispare subagentes.
