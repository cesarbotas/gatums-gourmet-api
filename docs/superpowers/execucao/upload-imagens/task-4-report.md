# Task 4 — relatório de implementação (endpoint POST /api/v1/imagens)

Implementer: subagent fresco (general-task-execution) + ajuste do orquestrador. Build: 0 erros.

## Arquivos criados/alterados
- Criados:
  - `src/GatumsGourmet.Api/Endpoints/V1/Imagens/ImagensRequest.cs` (`[FromForm(Name="arquivo")] IFormFile Arquivo`)
  - `src/GatumsGourmet.Api/Endpoints/V1/Imagens/ImagensEndpoints.cs`
- Alterado:
  - `src/GatumsGourmet.Api/Extensions/EndpointRouteBuilderExtensions.cs` (using + `ImagensEndpoints.Map(v1.MapGroup("/imagens").WithTags("Imagens"))`)

## Endpoint final
`POST /api/v1/imagens`, multipart/form-data, `[AsParameters] ImagensRequest`:
- guarda de borda: arquivo ausente/vazio → `Results.Problem("Arquivo é obrigatório.", 400)`.
- abre `request.Arquivo.OpenReadStream()` (`await using`), chama `IUploadImagemHandler.Handle(stream, ContentType, FileName, Length, ct)`.
- retorna `result.ToHttpResult(StatusCodes.Status201Created)`.
- encadeia `.RequirePermission("produtos:escrever").DisableAntiforgery()`.

## Decisões/convenções
- `.Accepts/.Produces/.WithName` OMITIDOS: nenhum endpoint do projeto usa; padrão é minimalista (handler + ToHttpResult + RequirePermission). `.DisableAntiforgery()` é requisito técnico do multipart em Minimal API.
- **Passa `request.Arquivo.Length` como `tamanho`** → resolve o minor diferido da Task 2 (tamanho real como fonte de verdade).

## Ajuste do orquestrador (pós-implementação, pré-commit)
- O implementer tinha usado `Results.BadRequest(new { erro = ... })` na guarda de borda. Troquei por `Results.Problem(..., 400)` para manter o MESMO shape ProblemDetails dos demais erros 400 do projeto (via `ResultExtensions.ToHttpResult` → `Results.Problem`). Rebuild 0 erros. Motivo: consistência do contrato de erro (ver docs/Site/Erros-e-Padroes). Custo se errado: baixo.

## Dívidas deferidas
- Testes do endpoint (201/403/401/400) → Task 5.
