# Padronização dos endpoints para o padrão [AsParameters]

Feature pequena/mecânica (sem SDD completo). Branch `feature/padronizar-endpoints-asparameters`.
Origem: dívida registrada na feature upload-imagens — o usuário tinha criado `ProdutosRequest`
(query params agrupados + `[AsParameters]`) e pediu replicar o padrão nas demais classes de endpoint.

## Decisão do usuário
- Opção **(a)**: replicar a estrutura de subpasta do Produtos (`Endpoints/V1/<Recurso>/{<Recurso>Endpoints,<Recurso>Request}.cs`),
  não só agrupar os query params. Mais consistente com Produtos/Imagens.
- Escopo: o padrão `[AsParameters]` vale só para os GET de **listagem** (múltiplos `[FromQuery]`).
  POST/PUT/DELETE (`[FromBody]`) e rotas com `{id}` ficam como estão.

## O que foi feito
Movidos para subpasta + criado `<Recurso>Request` com os query params da listagem:
- **Categorias** → `Endpoints/V1/Categorias/{CategoriasEndpoints,CategoriasRequest}.cs`
  - `CategoriasRequest`: `pagina`, `tamanho`, `apenasAtivas` (bool?). Record `ListCategoriasRequest(Pagina, Tamanho, ApenasAtivas)`.
- **Clientes** → `Endpoints/V1/Clientes/{ClientesEndpoints,ClientesRequest}.cs`
  - `ClientesRequest`: `pagina`, `tamanho`, `search`. Record `ListClientesRequest(Page, PageSize, Search)` (nomes diferem; mapeamento posicional preservado).
- **Pedidos** → `Endpoints/V1/Pedidos/{PedidosEndpoints,PedidosRequest}.cs`
  - `PedidosRequest`: `pagina`, `tamanho`. O `ClaimsPrincipal` + lógica de autorização inline continuam parâmetros separados do endpoint (fora do `[AsParameters]`).
- `EndpointRouteBuilderExtensions`: adicionados os `using` das 3 novas subpastas.

Preservado em todos: o clamp inline `< 1 ? 1 : ...` / `< 1 ? 10 : ...` para pagina/tamanho; nomes de query em minúsculo via `[FromQuery(Name="...")]` (igual ProdutosRequest).

## Verificação
- Build da Api: 0 erros.
- Testes de integração das 3 listagens (ListClientes/ListCategorias/ListPedidosEndpointTests): 7/7 passed — cobrem `?pagina=&tamanho=` (binding [AsParameters] exercitado).
- Suíte de integração COMPLETA: 104/104 passed (zero regressão).

## Status
Concluída. Arquivos antigos flat (`CategoriasEndpoints.cs`/`ClientesEndpoints.cs`/`PedidosEndpoints.cs` em Endpoints/V1/) removidos pelos moves.
