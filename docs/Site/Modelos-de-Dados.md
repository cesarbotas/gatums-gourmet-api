# Modelos de Dados (DTOs)

Todos os DTOs da API. Propriedades em camelCase (padrão do System.Text.Json).

> `Guid` → string · `decimal` → number · `DateTime` → string ISO-8601 · `bool` → boolean · enums → ver [Erros-e-Padroes.md](Erros-e-Padroes.md#enums)

---

## Auth

### LoginRequest
| Campo | Tipo | Obrigatório |
|-------|------|------------|
| `email` | string | sim |
| `senha` | string | sim |

### LoginResponse
| Campo | Tipo |
|-------|------|
| `token` | string |
| `user` | LoginUserDto |

### LoginUserDto
| Campo | Tipo |
|-------|------|
| `id` | string (Guid) |
| `nome` | string |
| `email` | string |
| `role` | string (`"admin"` ou `"cliente"`) |

### RegisterUserRequest
| Campo | Tipo | Obrigatório |
|-------|------|------------|
| `nome` | string | sim |
| `email` | string | sim |
| `senha` | string | sim |
| `telefone` | string? | não |

### RegisterUserResponse
| Campo | Tipo |
|-------|------|
| `id` | string (Guid) |
| `nome` | string |
| `email` | string |
| `role` | string (sempre `"cliente"`) |

### GetCurrentUserResponse
| Campo | Tipo |
|-------|------|
| `id` | string (Guid) |
| `nome` | string |
| `email` | string |
| `role` | string |

---

## Produtos

### ProdutoSummaryResponse (lista)
| Campo | Tipo |
|-------|------|
| `id` | string (Guid) |
| `nome` | string |
| `descricao` | string |
| `categoria` | string |
| `preco` | number (decimal) |
| `fotoUrl` | string |
| `disponivel` | boolean |
| `tipo` | string (`"simples"` ou `"combo"`) |

### ProdutoDetailResponse (detalhe)
| Campo | Tipo |
|-------|------|
| `id` | string (Guid) |
| `nome` | string |
| `descricao` | string |
| `categoria` | string |
| `preco` | number |
| `fotoUrl` | string |
| `disponivel` | boolean |
| `tipo` | string |
| `itens` | ComboItemResponse[] |

### ComboItemResponse
| Campo | Tipo |
|-------|------|
| `produtoId` | string (Guid) |
| `quantidade` | number (int) |

### CreateProdutoRequest
| Campo | Tipo | Obrigatório | Observação |
|-------|------|------------|------------|
| `nome` | string | sim | |
| `descricao` | string | sim | |
| `categoria` | string | sim | |
| `preco` | number | sim | |
| `fotoUrl` | string | sim | |
| `disponivel` | boolean | sim | |
| `tipo` | int (enum) | sim | `0` = Simples, `1` = Combo |
| `itens` | ComboItemInput[]? | se combo | Obrigatório e não vazio quando tipo=Combo |

### ComboItemInput
| Campo | Tipo |
|-------|------|
| `produtoId` | string (Guid) |
| `quantidade` | number (int) |

### CreateProdutoResponse
| Campo | Tipo |
|-------|------|
| `id` | string (Guid) |

### UpdateProdutoRequest
| Campo | Tipo | Observação |
|-------|------|------------|
| `id` | string (Guid) | Sobrescrito pelo id da rota |
| `nome` | string | |
| `descricao` | string | |
| `categoria` | string | |
| `preco` | number | |
| `fotoUrl` | string | |
| `disponivel` | boolean | |

> UpdateProdutoRequest não inclui `tipo` nem `itens`.

---

## Clientes

### ClienteResponse
| Campo | Tipo |
|-------|------|
| `id` | string (Guid) |
| `nome` | string |
| `email` | string |
| `telefone` | string? |
| `endereco` | EnderecoResponse |
| `criadoEm` | string (DateTime) |

### EnderecoResponse
| Campo | Tipo |
|-------|------|
| `id` | string (Guid) |
| `clienteId` | string (Guid) |
| `logradouro` | string |
| `numero` | string |
| `complemento` | string? |
| `bairro` | string |
| `cidade` | string |
| `estado` | string |
| `cep` | string |

### CreateClienteRequest
| Campo | Tipo | Obrigatório | Observação |
|-------|------|------------|------------|
| `nome` | string | sim | máx 100 |
| `email` | string | sim | formato email |
| `telefone` | string? | não | |
| `endereco` | EnderecoRequest | sim | |

### EnderecoRequest
| Campo | Tipo | Observação |
|-------|------|------------|
| `logradouro` | string | máx 200 |
| `numero` | string | máx 20 |
| `complemento` | string? | |
| `bairro` | string | máx 100 |
| `cidade` | string | máx 100 |
| `estado` | string | exatamente 2 caracteres |
| `cep` | string | máx 8 caracteres |

### CreateClienteResponse
| Campo | Tipo |
|-------|------|
| `id` | string (Guid) |

### UpdateClienteRequest
| Campo | Tipo | Observação |
|-------|------|------------|
| `id` | string (Guid) | Sobrescrito pelo id da rota |
| `nome` | string | |
| `email` | string | |
| `telefone` | string? | |

> UpdateClienteRequest não inclui `endereco`.

---

## Carrinho

### CarrinhoResponse
| Campo | Tipo |
|-------|------|
| `itens` | CarrinhoItemResponse[] |
| `total` | number (decimal) |

### CarrinhoItemResponse
| Campo | Tipo |
|-------|------|
| `produtoId` | string (Guid) |
| `nome` | string |
| `preco` | number (decimal) |
| `fotoUrl` | string |
| `quantidade` | number (int) |

### AddCarrinhoItemRequest
| Campo | Tipo |
|-------|------|
| `produtoId` | string (Guid) |
| `quantidade` | number (int) |

### UpdateCarrinhoItemQuantidadeRequest
| Campo | Tipo |
|-------|------|
| `quantidade` | number (int) |

---

## Pedidos

### CheckoutPedidoResponse
| Campo | Tipo |
|-------|------|
| `pedidoId` | string (Guid) |

### PedidoResponse
| Campo | Tipo |
|-------|------|
| `id` | string (Guid) |
| `userId` | string (Guid) |
| `clienteNome` | string |
| `itens` | PedidoItemResponse[] |
| `total` | number (decimal) |
| `data` | string (DateTime) |
| `statusAtual` | string (nome da etapa, lowercase) |
| `timeline` | TimelineEventoResponse[] |
| `pagamento` | PedidoPagamentoResponse? |

### PedidoItemResponse
| Campo | Tipo |
|-------|------|
| `produtoId` | string (Guid) |
| `nome` | string |
| `preco` | number (decimal) |
| `foto` | string |
| `quantidade` | number (int) |

### TimelineEventoResponse
| Campo | Tipo | Observação |
|-------|------|------------|
| `etapa` | string | Nome da etapa |
| `em` | string (DateTime) | Momento do evento |
| `por` | string | `"cliente"` ou `"admin"` |
| `observacao` | string? | |
| `entregaTipo` | string? | `"transporte"` ou `"retirada"` |

### PedidoPagamentoResponse
| Campo | Tipo |
|-------|------|
| `transacaoId` | string (Guid) |
| `resumo` | string |
| `aprovadoEm` | string (DateTime) |

### AvancarPedidoEtapaRequest
| Campo | Tipo | Observação |
|-------|------|------------|
| `proximaEtapa` | int (enum) | Ver [enums](Erros-e-Padroes.md#enums) |
| `observacao` | string? | |
| `entregaTipo` | int? (enum) | Necessário para etapa `Entregue` |

### ConcluirPagamentoPedidoRequest
| Campo | Tipo |
|-------|------|
| `transacaoId` | string (Guid) |

---

## Pagamentos

### OpcaoPagamentoResponse
| Campo | Tipo | Valores |
|-------|------|--------|
| `metodo` | string | `"cartao_credito"`, `"cartao_debito"`, `"pix"` |
| `label` | string | |
| `disponivel` | boolean | |

> `metodo` usa underscore aqui. Em `IniciarPagamentoRequest`, o enum espera **int**.

### IniciarPagamentoRequest
| Campo | Tipo | Valores |
|-------|------|--------|
| `pedidoId` | string (Guid) | |
| `metodo` | int (enum) | `0` = CartaoDebito, `1` = CartaoCredito, `2` = Pix |
| `valor` | number (decimal) | |

### ProcessarPagamentoCartaoRequest
| Campo | Tipo |
|-------|------|
| `numero` | string |
| `nome` | string |
| `validade` | string |
| `cvv` | string |

### TransacaoResponse
| Campo | Tipo | Observação |
|-------|------|------------|
| `id` | string (Guid) | ID da transação |
| `pedidoId` | string (Guid) | |
| `metodo` | string | lowercase sem underscore (`"cartaocredito"`, `"pix"`, etc.) |
| `valor` | number (decimal) | |
| `status` | string | `"pendente"`, `"aprovado"`, `"recusado"`, `"expirado"` |
| `pixCopiaCola` | string? | Apenas quando metodo=Pix |
| `pixExpiraEm` | string? (DateTime) | Apenas quando metodo=Pix |
| `cartaoUltimos4` | string? | Apenas cartão |
| `cartaoBandeira` | string? | Apenas cartão |
| `mensagemErro` | string? | Se recusado |
| `criadoEm` | string (DateTime) | |
| `atualizadoEm` | string (DateTime) | |

---

## Relatórios

### VendasResumoResponse
| Campo | Tipo |
|-------|------|
| `faturamento` | number (decimal) |
| `totalPedidos` | number (int) |
| `ticketMedio` | number (decimal) |
| `topProdutos` | TopProdutoResponse[] |
| `serieDiaria` | VendaDiariaResponse[] |

### TopProdutoResponse
| Campo | Tipo |
|-------|------|
| `produtoId` | string (Guid) |
| `nome` | string |
| `quantidade` | number (int) |
| `total` | number (decimal) |

### VendaDiariaResponse
| Campo | Tipo |
|-------|------|
| `data` | string (`"yyyy-MM-dd"`) |
| `faturamento` | number (decimal) |
| `pedidos` | number (int) |

---

## Tipos comuns

### PaginatedResponse<T>
| Campo | Tipo |
|-------|------|
| `items` | T[] |
| `total` | number (int) |
| `page` | number (int) |
| `pageSize` | number (int) |
