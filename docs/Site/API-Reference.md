# API Reference — Todos os Endpoints

Base URL: `{API_BASE}/api/v1`

## Autorização (RBAC + refresh token)

> **Mudança de contrato (auth):** o login deixou de devolver um JWT único de 8h.
> Agora devolve **access token curto (~15 min) + refresh token rotativo (~7 dias)**,
> e o acesso administrativo passou a ser por **permissões** (`recurso:acao`)
> embutidas no access token. Ver [Guia de uso](../Auth/Guia-de-Uso.md),
> [Guia de permissões](../Auth/Guia-Permissoes.md) e as
> [ADRs](../adr/).

Modelo de acesso:

- **público** — sem token.
- **Bearer** — exige `Authorization: Bearer <accessToken>` (qualquer usuário
  autenticado).
- **permissão** (ex.: `produtos:escrever`) — exige Bearer **e** que o access
  token carregue a permissão indicada (`.RequirePermission("...")`). Sem a
  permissão → `403`.
- **ownership** — Bearer + o recurso precisa pertencer ao usuário do token
  (checagem `UsuarioId == userId` no handler).

Tokens:

- `accessToken`: JWT HS256, ~15 min (`Jwt:AccessTokenMinutes`). Claims: `sub`,
  `email`, `jti`, uma claim de role por role (informativa) e uma claim `perm` por
  permissão.
- `refreshToken`: valor opaco ~7 dias (`Jwt:RefreshTokenDays`), hasheado no banco,
  rotativo com detecção de reúso (reapresentar um refresh revogado revoga a
  família inteira).

## Auth

### POST /auth/login
- **Auth**: público
- **Body**: `LoginRequest`
  ```json
  { "email": "string", "senha": "string" }
  ```
- **200**: `LoginResponse`
  ```json
  {
    "accessToken": "string",
    "refreshToken": "string",
    "expiresIn": 900,
    "user": { "id": "guid", "nome": "string", "email": "string", "roles": ["Admin"] }
  }
  ```
  > `expiresIn` é a validade do access **em segundos**. `user.roles` é uma lista
  > de nomes de role.
- **400**: `"Credenciais inválidas."` ou `"Usuário inativo."`

### POST /auth/register
- **Auth**: público
- **Body**: `RegisterUserRequest`
  ```json
  { "nome": "string", "email": "string", "senha": "string", "telefone": "string|null" }
  ```
  > Cria sempre um usuário **Cliente**. Para criar usuários com outras roles, use
  > `POST /usuarios` (`usuarios:gerenciar`).
- **201**: `RegisterUserResponse`
  ```json
  { "id": "guid", "nome": "string", "email": "string", "roles": ["Cliente"] }
  ```

### POST /auth/refresh
- **Auth**: público (valida o próprio refresh token)
- **Body**: `RefreshTokenRequest`
  ```json
  { "refreshToken": "string" }
  ```
- **200**: `LoginResponse` (mesmo shape do login — par de tokens renovado)
  > **Rotação:** o refresh é de uso único; cada chamada invalida o refresh antigo
  > e devolve um novo. Substitua sempre o refresh guardado.
- **400**: `"Refresh token inválido."` (inclui reúso detectado → família revogada)
  ou `"Refresh token expirado."` ou `"Usuário inativo."`

### GET /auth/me
- **Auth**: Bearer
- **200**: `GetCurrentUserResponse`
  ```json
  { "id": "guid", "nome": "string", "email": "string", "roles": ["Admin"] }
  ```

### POST /auth/logout
- **Auth**: Bearer
- **Body**: `LogoutRequest`
  ```json
  { "refreshToken": "string" }
  ```
  > Revoga a família do refresh informado. O access em curso vale até expirar.
- **200**: `true`

### POST /auth/logout-all
- **Auth**: Bearer
- **Sem body**
  > Revoga todos os refresh ativos do usuário (útil após troca de senha).
- **200**: `true`

### POST /auth/maintenance/purge-tokens
- **Auth**: `usuarios:gerenciar`
- **Sem body**
  > Gatilho manual do housekeeping: apaga refresh tokens expirados/revogados fora
  > da retenção. O mesmo serviço roda automaticamente em background. Ver
  > [ADR 0004](../adr/0004-limpeza-refresh-tokens.md).
- **200**: resultado da limpeza

---

## Produtos

> **Mudança de contrato:** categoria deixou de ser texto livre e passou a ser uma
> entidade própria (ver seção [Categorias](#categorias)). Produtos referenciam a
> categoria por `categoriaId` (number). O antigo `GET /produtos/categorias` foi
> **removido** — use `GET /categorias`.

### GET /produtos
- **Auth**: público
- **Query params**:

  | Param | Tipo | Default | Descrição |
  |-------|------|---------|-----------|
  | `pagina` | int | 1 | Página (1-based) |
  | `tamanho` | int | 10 | Itens por página |
  | `search` | string? | — | Busca no nome/descrição |
  | `categoriaId` | number? | — | Filtro por id da categoria |
  | `tipo` | string? | — | `simples` ou `combo` (case-insensitive) |
  | `disponivel` | bool? | — | Filtra disponibilidade |

  Exemplo: `GET /produtos?pagina=1&tamanho=20&categoriaId=1&disponivel=true`

- **200**: `PaginatedResponse<ProdutoSummaryResponse>`
  ```json
  {
    "itens": [
      {
        "id": "guid",
        "nome": "Hambúrguer Artesanal",
        "descricao": "Blend 180g, queijo cheddar...",
        "categoriaId": 1,
        "categoriaNome": "Hambúrgueres",
        "preco": 32.90,
        "fotoUrl": "https://...",
        "disponivel": true,
        "tipo": "simples"
      }
    ],
    "paginaAtual": 1,
    "tamanhoPagina": 10,
    "totalRegistros": 25,
    "totalPaginas": 3,
    "temPaginaAnterior": false,
    "temProximaPagina": true
  }
  ```

### GET /produtos/{id}
- **Auth**: público
- **Route**: `id` (Guid)
- **200**: `ProdutoDetailResponse`
  ```json
  {
    "id": "guid",
    "nome": "string",
    "descricao": "string",
    "categoriaId": 1,
    "categoriaNome": "string",
    "preco": 89.90,
    "fotoUrl": "string",
    "disponivel": true,
    "tipo": "combo",
    "itens": [
      { "produtoId": "guid", "quantidade": 2 }
    ]
  }
  ```
- **400**: `"Produto não encontrado."`

### POST /produtos
- **Auth**: `produtos:escrever`
- **Body**: `CreateProdutoRequest`
  ```json
  {
    "nome": "string",
    "descricao": "string",
    "categoriaId": 1,
    "preco": 89.90,
    "fotoUrl": "string",
    "disponivel": true,
    "tipo": 1,
    "itens": [
      { "produtoId": "guid", "quantidade": 2 }
    ]
  }
  ```
  > `categoriaId` (number): id de uma categoria existente (ver `GET /categorias`). Se a categoria não existir → `400 "Categoria não encontrada."`
  > `tipo`: `0` = Simples, `1` = Combo (inteiros). Combo exige `itens[]` não vazio.
- **201**: `CreateProdutoResponse` → `{ "id": "guid" }`

### PUT /produtos/{id}
- **Auth**: `produtos:escrever`
- **Route**: `id` (Guid)
- **Body**: `UpdateProdutoRequest`
  ```json
  {
    "id": "guid",
    "nome": "string",
    "descricao": "string",
    "categoriaId": 1,
    "preco": 94.90,
    "fotoUrl": "string",
    "disponivel": true
  }
  ```
  > O `id` do body é sobrescrito pelo `id` da rota. Não inclui `tipo`/`itens`.
  > `categoriaId` deve referenciar categoria existente, senão `400 "Categoria não encontrada."`
- **200**: `true` (boolean)

### DELETE /produtos/{id}
- **Auth**: `produtos:escrever`
- **200**: `true` (boolean)

---

## Imagens

Upload de imagem de produto para storage S3-compatível. Endpoint separado do CRUD
de produtos: o front faz o upload, recebe a URL pública e grava **só a string**
em `fotoUrl` no produto. Guia de consumo no front: [Upload-Imagens.md](Upload-Imagens.md).

### POST /imagens
- **Auth**: `produtos:escrever`
- **Content-Type**: `multipart/form-data`
- **Body**: campo `arquivo` (um único arquivo)
  > Tipos aceitos: **JPEG / PNG / WebP** — detectados por **magic bytes** (o
  > `Content-Type` e o nome do arquivo não decidem o tipo). Tamanho máximo **5 MB**
  > (`Storage:MaxFileSizeMB`).
- **201**: `UploadImagemResponse`
  ```json
  { "url": "https://.../produtos/{guid}.jpg" }
  ```
  > O object key é gerado pela API como `produtos/{guid}.{ext}`. Use a `url` como
  > `fotoUrl` ao criar/atualizar o produto.
- **400**: `"Tipo de imagem não suportado. Use JPEG, PNG ou WebP."`, `"Arquivo vazio."`
  ou `"Arquivo excede o tamanho máximo de 5 MB."` (ProblemDetails)
- **500**: storage não configurado no ambiente (mensagem clara)

---

## Categorias

Entidade de catálogo das categorias de produto. `id` é um inteiro (number, long).

### GET /categorias
- **Auth**: público
- **Query params**:

  | Param | Tipo | Default | Descrição |
  |-------|------|---------|-----------|
  | `pagina` | int | 1 | Página (1-based) |
  | `tamanho` | int | 10 | Itens por página |
  | `apenasAtivas` | bool? | — | `true` traz só as categorias ativas (uso típico do catálogo) |

- **200**: `PaginatedResponse<CategoriaResponse>`
  ```json
  {
    "itens": [
      { "id": 1, "nome": "Hambúrgueres", "descricao": "Lanches artesanais", "ativo": true },
      { "id": 2, "nome": "Bebidas", "descricao": null, "ativo": true }
    ],
    "paginaAtual": 1,
    "tamanhoPagina": 10,
    "totalRegistros": 2,
    "totalPaginas": 1,
    "temPaginaAnterior": false,
    "temProximaPagina": false
  }
  ```

### GET /categorias/{id}
- **Auth**: público
- **Route**: `id` (number)
- **200**: `CategoriaResponse`
- **400**: `"Categoria não encontrada."`

### POST /categorias
- **Auth**: `categorias:escrever`
- **Body**: `CreateCategoriaRequest`
  ```json
  { "nome": "string", "descricao": "string|null", "ativo": true }
  ```
- **201**: `CreateCategoriaResponse` → `{ "id": 1 }`
- **400**: `"Já existe uma categoria com este nome."` (nome é único)

### PUT /categorias/{id}
- **Auth**: `categorias:escrever`
- **Route**: `id` (number)
- **Body**: `UpdateCategoriaRequest`
  ```json
  { "id": 1, "nome": "string", "descricao": "string|null", "ativo": true }
  ```
  > O `id` do body é sobrescrito pelo `id` da rota.
- **200**: `true`
- **400**: `"Categoria não encontrada."` ou `"Já existe uma categoria com este nome."`

### DELETE /categorias/{id}
- **Auth**: `categorias:escrever`
- **Route**: `id` (number)
- **200**: `true`
- **400**: `"Categoria não encontrada."` ou `"Não é possível excluir: categoria possui produtos vinculados."`

---

## Clientes (CRM admin)

### GET /clientes
- **Auth**: `clientes:ler`
- **Query params**: `pagina` (int, default 1), `tamanho` (int, default 10), `search` (string?)
- **200**: `PaginatedResponse<ClienteResponse>`
  ```json
  {
    "itens": [
      {
        "id": "guid",
        "nome": "string",
        "email": "string",
        "telefone": "string|null",
        "endereco": {
          "id": "guid",
          "clienteId": "guid",
          "logradouro": "string",
          "numero": "string",
          "complemento": "string|null",
          "bairro": "string",
          "cidade": "string",
          "estado": "string",
          "cep": "string"
        },
        "criadoEm": "2026-10-20T12:00:00Z"
      }
    ],
    "paginaAtual": 1,
    "tamanhoPagina": 10,
    "totalRegistros": 10,
    "totalPaginas": 1,
    "temPaginaAnterior": false,
    "temProximaPagina": false
  }
  ```

### POST /clientes
- **Auth**: `clientes:escrever`
- **Body**: `CreateClienteRequest`
  ```json
  {
    "nome": "string",
    "email": "string",
    "telefone": "string|null",
    "endereco": {
      "logradouro": "string",
      "numero": "string",
      "complemento": "string|null",
      "bairro": "string",
      "cidade": "string",
      "estado": "SP",
      "cep": "01000000"
    }
  }
  ```
  > `estado` deve ter 2 caracteres. `cep` máx 8 caracteres.
- **201**: `CreateClienteResponse` → `{ "id": "guid" }`

### PUT /clientes/{id}
- **Auth**: `clientes:escrever`
- **Body**: `UpdateClienteRequest`
  ```json
  { "id": "guid", "nome": "string", "email": "string", "telefone": "string|null" }
  ```
  > Não inclui `endereco`.
- **200**: `true`

### DELETE /clientes/{id}
- **Auth**: `clientes:escrever`
- **200**: `true`

---

## Endereços (CEP)

Consulta pública de endereço por CEP (autopreenchimento de formulário). Read-only.
Guia de consumo no front: [Busca-CEP.md](Busca-CEP.md).

### GET /enderecos/cep/{cep}
- **Auth**: público
- **Route**: `cep` (8 dígitos, com ou sem máscara — `01001000` ou `01001-000`)
- **200**: `EnderecoCepResponse`
  ```json
  {
    "cep": "01001000",
    "logradouro": "Praça da Sé",
    "complemento": "lado ímpar",
    "bairro": "Sé",
    "cidade": "São Paulo",
    "estado": "SP"
  }
  ```
  > `complemento` pode ser `null`. `numero` não é retornado (o ViaCEP não fornece).
- **400**: `"CEP inválido."` (formato), `"CEP não encontrado."`, ou `"Serviço de CEP indisponível."`

---

## Carrinho

> **Auth**: Bearer (autenticado) + **ownership**. O `userId` é extraído
> automaticamente do token; cada usuário só enxerga/mexe no próprio carrinho.

### GET /carrinho
- **200**: `CarrinhoResponse`
  ```json
  {
    "itens": [
      {
        "produtoId": "guid",
        "nome": "string",
        "preco": 32.90,
        "fotoUrl": "string",
        "quantidade": 2
      }
    ],
    "total": 65.80
  }
  ```

### POST /carrinho/itens
- **Body**: `AddCarrinhoItemRequest`
  ```json
  { "produtoId": "guid", "quantidade": 2 }
  ```
- **200**: `true`

### PATCH /carrinho/itens/{produtoId}
- **Route**: `produtoId` (Guid)
- **Body**: `UpdateCarrinhoItemQuantidadeRequest`
  ```json
  { "quantidade": 3 }
  ```
- **200**: `true`

### DELETE /carrinho/itens/{produtoId}
- **Route**: `produtoId` (Guid)
- **200**: `true`

### DELETE /carrinho
- **200**: `true` (limpa todo o carrinho)

---

## Pedidos

### POST /pedidos/checkout
- **Auth**: Bearer + ownership (pedido criado para o usuário do token)
- **Body**: `{ "clienteNome": "string" }`
- **201**: `CheckoutPedidoResponse` → `{ "pedidoId": "guid" }`
  > Cria o pedido a partir do carrinho atual. Etapa inicial: `criado`.

### GET /pedidos
- **Auth**: Bearer
- **Query params**: `pagina` (int, default 1), `tamanho` (int, default 10)
- **Comportamento**: quem está na role `Admin` vê todos; os demais veem apenas os
  próprios (filtro automático por userId do token).
- **200**: `PaginatedResponse<PedidoResponse>` (ver [Modelos-de-Dados.md](Modelos-de-Dados.md#pedidos))

### GET /pedidos/{id}
- **Auth**: Bearer
- **Comportamento**: role `Admin` vê qualquer pedido; os demais só o próprio
  (recebe 400 se não for dono).
- **200**: `PedidoResponse`

### POST /pedidos/{id}/etapas/avancar
- **Auth**: `pedidos:avancar`
- **Body**: `AvancarPedidoEtapaRequest`
  ```json
  {
    "proximaEtapa": 1,
    "observacao": "string|null",
    "entregaTipo": null
  }
  ```
  > `proximaEtapa` (int): ver [enums](Erros-e-Padroes.md#enums). O actor é derivado
  > do token: quem está na role `Admin` age como Admin, senão como Cliente.
  > `entregaTipo` (int|null): necessário quando a etapa é `Entregue` (5).
- **200**: `true`

### POST /pedidos/{id}/pagamento/concluir
- **Auth**: Bearer + ownership
- **Body**: `ConcluirPagamentoPedidoRequest`
  ```json
  { "transacaoId": "guid" }
  ```
- **200**: `true`

---

## Pagamentos

> **Auth dos pagamentos do próprio pedido** (`/opcoes`, `/iniciar`, `/{id}/cartao`,
> `/{id}/status`, `/{id}/pix/confirmar`): Bearer + ownership (transação do próprio
> pedido). Já `POST /{id}/validar` exige a permissão `pagamentos:validar`.

### GET /pagamentos/opcoes
- **Auth**: Bearer + ownership
- **200**: array de `OpcaoPagamentoResponse`
  ```json
  [
    { "metodo": "cartao_credito", "label": "Cartão de Crédito", "disponivel": true },
    { "metodo": "cartao_debito", "label": "Cartão de Débito", "disponivel": true },
    { "metodo": "pix", "label": "Pix", "disponivel": true }
  ]
  ```
  > Aviso: `metodo` aqui usa underscore. Em `/iniciar`, o enum espera **inteiro**.

### POST /pagamentos/iniciar
- **Auth**: Bearer + ownership
- **Header opcional**: `Idempotency-Key: string` (passa mas não é enforceado no mock)
- **Body**: `IniciarPagamentoRequest`
  ```json
  { "pedidoId": "guid", "metodo": 2, "valor": 89.90 }
  ```
  > `metodo` (int): `0` = CartaoDebito, `1` = CartaoCredito, `2` = Pix
- **201**: `TransacaoResponse` (ver [Modelos-de-Dados.md](Modelos-de-Dados.md#pagamentos))
  > Se já existe transação pendente para o pedido, retorna a existente. Para Pix, resposta inclui `pixCopiaCola` e `pixExpiraEm`.

### POST /pagamentos/{transacaoId}/cartao
- **Auth**: Bearer + ownership
- **Body**: `ProcessarPagamentoCartaoRequest`
  ```json
  {
    "numero": "4111111111111111",
    "nome": "JOÃO CLIENTE",
    "validade": "12/28",
    "cvv": "123"
  }
  ```
- **200**: `TransacaoResponse`

### GET /pagamentos/{transacaoId}/status
- **Auth**: Bearer + ownership
- **200**: `TransacaoResponse`

### POST /pagamentos/{transacaoId}/pix/confirmar
- **Auth**: Bearer + ownership
- **Sem body**
- **200**: `TransacaoResponse`

### POST /pagamentos/{transacaoId}/validar
- **Auth**: `pagamentos:validar`
- **Sem body**
- **200**: `TransacaoResponse`
  > Valida a transação para aprovação do pedido. Chamar antes de avançar para `PagamentoAprovado`.

---

## Relatórios

### GET /relatorios/vendas/resumo
- **Auth**: `relatorios:ler`
- **200**: `VendasResumoResponse`
  ```json
  {
    "faturamento": 15420.50,
    "totalPedidos": 87,
    "ticketMedio": 177.01,
    "topProdutos": [
      { "produtoId": "guid", "nome": "string", "quantidade": 45, "total": 1476.00 }
    ],
    "serieDiaria": [
      { "data": "2026-09-21", "faturamento": 520.00, "pedidos": 3 }
    ]
  }
  ```
  > Últimos 30 dias.
---

## Roles (administração)

> **Auth**: todas exigem a permissão `roles:gerenciar`. Roles de sistema
> (`Admin`, `Cliente`) não podem ser alteradas, excluídas nem ter permissões
> trocadas via API.

### GET /roles
- **Auth**: `roles:gerenciar`
- **200**: array de `RoleResponse`
  ```json
  [
    { "id": 1, "nome": "Admin", "descricao": "Acesso total ao sistema", "sistema": true, "permissoes": ["produtos:ler", "produtos:escrever", "..."] }
  ]
  ```

### GET /roles/{id}
- **Auth**: `roles:gerenciar`
- **Route**: `id` (int)
- **200**: `RoleResponse`
- **400**: `"Role não encontrada."`

### POST /roles
- **Auth**: `roles:gerenciar`
- **Body**: `CreateRoleRequest`
  ```json
  { "nome": "string", "descricao": "string|null" }
  ```
- **201**: `{ "id": 4 }`
- **400**: `"Já existe uma role com este nome."`

### PUT /roles/{id}
- **Auth**: `roles:gerenciar`
- **Route**: `id` (int)
- **Body**: `UpdateRoleRequest`
  ```json
  { "id": 4, "nome": "string", "descricao": "string|null" }
  ```
  > O `id` do body é sobrescrito pelo `id` da rota.
- **200**: `true`
- **400**: `"Role não encontrada."`, `"Roles de sistema não podem ser alteradas."` ou `"Já existe uma role com este nome."`

### DELETE /roles/{id}
- **Auth**: `roles:gerenciar`
- **Route**: `id` (int)
- **200**: `true`
- **400**: bloqueia roles de sistema ou roles com usuários vinculados.

### PUT /roles/{id}/permissions
- **Auth**: `roles:gerenciar`
- **Route**: `id` (int)
- **Body**: `SetRolePermissionsRequest` (define o conjunto completo, não é incremental)
  ```json
  { "permissionIds": [2, 3, 6, 7] }
  ```
- **200**: `true`
- **400**: `"Role não encontrada."` ou `"Permissões de roles de sistema são fixas."`

---

## Permissions (administração)

> **Auth**: todas exigem a permissão `permissoes:gerenciar`. CRUD do catálogo de
> permissões (uso de backoffice). Uma permissão sem rota que a verifique é inerte.

### GET /permissions
- **Auth**: `permissoes:gerenciar`
- **200**: array de `PermissionResponse`
  ```json
  [
    { "id": 1, "codigo": "produtos:ler", "descricao": "Visualizar produtos", "recurso": "produtos", "acao": "ler" }
  ]
  ```

### GET /permissions/{id}
- **Auth**: `permissoes:gerenciar`
- **Route**: `id` (int)
- **200**: `PermissionResponse`
- **400**: `"Permissão não encontrada."`

### POST /permissions
- **Auth**: `permissoes:gerenciar`
- **Body**: `CreatePermissionRequest`
  ```json
  { "codigo": "pedidos:cancelar", "descricao": "string|null", "recurso": "pedidos", "acao": "cancelar" }
  ```
  > `codigo` deve conter `:` (formato `recurso:acao`).
- **201**: `{ "id": 13 }`
- **400**: `"Já existe uma permissão com este código."`

### PUT /permissions/{id}
- **Auth**: `permissoes:gerenciar`
- **Route**: `id` (int)
- **Body**: `UpdatePermissionRequest` (o `id` do body é sobrescrito pelo da rota)
- **200**: `true`
- **400**: `"Permissão não encontrada."`

### DELETE /permissions/{id}
- **Auth**: `permissoes:gerenciar`
- **Route**: `id` (int)
- **200**: `true`
- **400**: `"Permissão não encontrada."`

---

## Usuarios (administração)

> **Auth**: todas exigem a permissão `usuarios:gerenciar`. Regra de segurança
> transversal: o sistema nunca pode ficar sem **administrador ativo** — remover a
> role Admin, desativar ou excluir o último admin ativo é bloqueado.

### GET /usuarios
- **Auth**: `usuarios:gerenciar`
- **200**: array de `UsuarioResponse`
  ```json
  [
    { "id": "guid", "nome": "string", "email": "string", "telefone": "string|null", "ativo": true, "roles": ["Admin"], "criadoEm": "2026-10-26T12:00:00Z" }
  ]
  ```

### GET /usuarios/{id}
- **Auth**: `usuarios:gerenciar`
- **Route**: `id` (Guid)
- **200**: `UsuarioResponse`
- **400**: `"Usuário não encontrado."`

### POST /usuarios
- **Auth**: `usuarios:gerenciar`
- **Body**: `CreateUsuarioRequest` (cria o usuário já com as roles informadas)
  ```json
  { "nome": "string", "email": "string", "senha": "string", "telefone": "string|null", "roleIds": [1] }
  ```
- **201**: `{ "id": "guid" }`
- **400**: `"E-mail já cadastrado."` ou `"Role {id} não encontrada."`

### PUT /usuarios/{id}/roles
- **Auth**: `usuarios:gerenciar`
- **Route**: `id` (Guid)
- **Body**: `SetUsuarioRolesRequest` (define o conjunto completo de roles)
  ```json
  { "roleIds": [1, 3] }
  ```
- **200**: `true`
- **400**: `"Usuário não encontrado."`, `"Role {id} não encontrada."` ou `"O sistema deve ter ao menos um administrador ativo."`

### PATCH /usuarios/{id}/ativo
- **Auth**: `usuarios:gerenciar`
- **Route**: `id` (Guid)
- **Body**: `SetUsuarioAtivoRequest`
  ```json
  { "ativo": false }
  ```
- **200**: `true`
- **400**: `"Usuário não encontrado."` ou `"O sistema deve ter ao menos um administrador ativo."`

### DELETE /usuarios/{id}
- **Auth**: `usuarios:gerenciar`
- **Route**: `id` (Guid)
  > **Soft-delete**: marca `Ativo=false` para preservar o histórico de pedidos.
- **200**: `true`
- **400**: `"O sistema deve ter ao menos um administrador ativo."` (ao tentar remover o último admin ativo)

---

## Matriz de rotas — pública vs. autenticada vs. permissão

| Rota | Acesso |
|---|---|
| POST `/auth/login`, `/auth/register`, `/auth/refresh` | pública |
| GET `/produtos`, `/produtos/{id}` | pública |
| GET `/categorias`, `/categorias/{id}` | pública |
| GET `/auth/me`, POST `/auth/logout`, `/auth/logout-all` | Bearer |
| Carrinho (todas) | Bearer + ownership |
| POST `/pedidos/checkout`, `/pedidos/{id}/pagamento/concluir` | Bearer + ownership |
| GET `/pedidos` | Bearer (role `Admin` vê todos; senão os próprios) |
| GET `/pedidos/{id}` | Bearer (dono **ou** role `Admin`) |
| Pagamentos do próprio pedido (`/opcoes`, `/iniciar`, `/{id}/cartao`, `/{id}/status`, `/{id}/pix/confirmar`) | Bearer + ownership |
| POST/PUT/DELETE `/produtos` | `produtos:escrever` |
| POST `/imagens` | `produtos:escrever` |
| POST/PUT/DELETE `/categorias` | `categorias:escrever` |
| GET `/clientes` | `clientes:ler` |
| POST/PUT/DELETE `/clientes` | `clientes:escrever` |
| POST `/pedidos/{id}/etapas/avancar` | `pedidos:avancar` |
| POST `/pagamentos/{id}/validar` | `pagamentos:validar` |
| GET `/relatorios/vendas/resumo` | `relatorios:ler` |
| `/roles` (CRUD + `/{id}/permissions`) | `roles:gerenciar` |
| `/permissions` (CRUD) | `permissoes:gerenciar` |
| `/usuarios` (CRUD + `/roles`, `/ativo`) | `usuarios:gerenciar` |
| POST `/auth/maintenance/purge-tokens` | `usuarios:gerenciar` |
