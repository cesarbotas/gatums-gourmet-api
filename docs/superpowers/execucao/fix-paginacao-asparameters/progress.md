# Fix: paginação com [AsParameters] int não-anulável virava query param OBRIGATÓRIO

Bug de regressão pego pelo Newman no CI. Teve DUAS rodadas (ver abaixo).

## Sintoma (CI / Newman)
```
Microsoft.AspNetCore.Http.BadHttpRequestException: Required parameter "int Pagina" was not provided from query string.
```
A coleção E2E chama os GET de listagem SEM `?pagina=&tamanho=`; o endpoint estourava 400 → ExceptionMiddleware → job vermelho.

## Causa raiz (systematic-debugging)
No Minimal API, um query param de **tipo-valor não-anulável** (`int Pagina`), numa classe `[AsParameters]` ou solto (`[FromQuery] int`), é **OBRIGATÓRIO**. O `= 1` na propriedade NÃO salva — o binding exige o valor na query. Sem ele → `BadHttpRequestException`.
Os testes de integração existentes sempre chamavam COM query params, então nunca pegaram; só o Newman expôs.

## Histórico (importante)
- **1ª rodada** (branch `fix/paginacao-asparameters-obrigatorio`): corrigi Produtos (`int?`) + os 3 flat. PR do fix foi aberta mas NÃO foi mergeada.
- **Mergeou primeiro a PR #36** (padronização `[AsParameters]`), que converteu Categorias/Clientes/Pedidos para `[AsParameters]` com `int` não-anulável E reverteu o ProdutosRequest para `int`. Resultado: o bug voltou/ampliou nos 4 endpoints no develop.
- **2ª rodada** (branch `fix/paginacao-opcional-asparameters`, a partir do develop pós-#36): re-apliquei o fix uniforme nos 4 (agora todos `[AsParameters]`).

## Correção (2ª rodada, a que vale)
- `ProdutosRequest`, `CategoriasRequest`, `ClientesRequest`, `PedidosRequest`: `Pagina`/`Tamanho` → `int?`.
- Os 4 endpoints de listagem: default/clamp `request.Pagina is null or < 1 ? 1 : request.Pagina.Value` (idem Tamanho com 10). NÃO usar `(x ?? 1) < 1 ? 1 : x!.Value` — isso estourava NRE quando null (pego na 1ª rodada, virava 500).

## TDD
- RED: `tests/.../Api/Paginacao/ListagemSemQueryParamsTests.cs` — chama os 4 GET SEM query params, espera 200. Reproduziu o 400 do CI (4/4 falhando).
- GREEN: `int?` + clamp `is null or < 1`. 4/4 passam.

## Verificação
- Build solution: 0 erros. Unit: 160/160. Integração: **108/108** (104 + 4 de regressão).

## Status
Concluído. Branch `fix/paginacao-opcional-asparameters`. Pronto para PR no develop (CI do develop está vermelho de novo por causa da #36 sem o fix).
Lição: PRs que tocam os mesmos arquivos devem ser rebaseadas antes do merge; a #36 foi mergeada sem o fix e reintroduziu o bug.
