# Task 1: Contrato IImageStorage + detecção de tipo por magic bytes

**Projeto:** API .NET 10 (Clean Architecture), branch feature/gravar-imagem-s3, em c:\Git\Botas\gatums-gourmet-api. Primeira task da feature de upload de imagens.

**Objetivo:** criar o contrato de storage no Domain e o detector de tipo de imagem por magic bytes na Application. São a base das próximas tasks.

## Constraints globais
- net10.0, noBOM, uma classe por arquivo. Seguir o padrão do repo.
- Domain não depende de nada; Application depende só de Domain.

## Arquivos

### 1. `src/GatumsGourmet.Domain/Services/Interfaces/IImageStorage.cs`
```csharp
namespace GatumsGourmet.Domain.Services.Interfaces;

public record ImageUploadResult(string Url, string ObjectKey);

public interface IImageStorage
{
    Task<ImageUploadResult> UploadAsync(Stream conteudo, string contentType, string extensao, string prefixo, CancellationToken ct = default);
}
```
(Pode ter o record e a interface no mesmo arquivo — é o padrão do repo para tipos pequenos relacionados, ex.: LoginResponse. Se preferir separar, ok, mas 1 responsabilidade por arquivo.)

### 2. `src/GatumsGourmet.Application/UseCases/V1/Imagens/ImageTypeDetector.cs`
```csharp
namespace GatumsGourmet.Application.UseCases.V1.Imagens;

public static class ImageTypeDetector
{
    // Detecta o tipo pela assinatura (magic bytes) do início do arquivo.
    // Retorna Ok=false se não reconhecer JPEG/PNG/WebP.
    public static (bool Ok, string Extensao, string ContentType) Detectar(ReadOnlySpan<byte> prefixo);
}
```
Regras de assinatura:
- JPEG: bytes 0-2 = `FF D8 FF` → ("jpg", "image/jpeg").
- PNG: bytes 0-7 = `89 50 4E 47 0D 0A 1A 0A` → ("png", "image/png").
- WebP: bytes 0-3 = `52 49 46 46` ("RIFF") E bytes 8-11 = `57 45 42 50` ("WEBP") → ("webp", "image/webp"). (Precisa de pelo menos 12 bytes.)
- Qualquer outra coisa (ou buffer curto demais) → (false, "", "").

## Teste (TDD) — `tests/GatumsGourmet.UnitTests/Application/UseCases/V1/Imagens/ImageTypeDetectorTests.cs`
`[Theory]` com `[InlineData]` ou `[MemberData]` (use `byte[]`):
1. JPEG `new byte[]{0xFF,0xD8,0xFF,0xE0}` → Ok=true, "jpg", "image/jpeg".
2. PNG `new byte[]{0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A}` → Ok=true, "png", "image/png".
3. WebP `new byte[]{0x52,0x49,0x46,0x46,0,0,0,0,0x57,0x45,0x42,0x50}` → Ok=true, "webp", "image/webp" (REVIEW FOCUS — assinatura menos óbvia).
4. texto `new byte[]{0x48,0x65,0x6C,0x6C,0x6F}` ("Hello") → Ok=false.
5. buffer vazio `Array.Empty<byte>()` → Ok=false.
Use FluentAssertions + xUnit (padrão do repo).

Passos TDD: escrever o teste → rodar e ver falhar (`dotnet test tests/GatumsGourmet.UnitTests --filter ImageTypeDetectorTests`) → implementar → rodar e ver passar.

## Validação
`dotnet build GatumsGourmet.sln` 0 erros; o teste filtrado passa. Se a API estiver rodando e travar DLLs (MSB3021/MSB3027), encerre o processo GatumsGourmet.Api e rebuilde.

## Commit
`feat: contrato IImageStorage e deteccao de tipo por magic bytes`

## Não dispare subagentes. Faça tudo você mesmo.
