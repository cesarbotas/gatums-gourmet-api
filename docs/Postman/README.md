# Postman — Gatums Gourmet API

Coleção e ambiente para testar todos os endpoints da API Gatums Gourmet (.NET 8).

## Arquivos

| Arquivo | Descrição |
|---------|-----------|
| `GatumsGourmet.postman_collection.json` | Coleção completa (34 endpoints) com scripts de teste que capturam tokens e IDs automaticamente |
| `GatumsGourmet-Local.environment.json` | Ambiente local com variáveis de conexão e credenciais seed |

## Como importar

1. Abra o Postman.
2. **Import** → arraste os dois arquivos (ou clique em *Upload Files*).
3. Selecione o ambiente **Gatums Gourmet - Local** no seletor no canto superior direito.

## Variáveis da coleção

| Variável | Descrição |
|----------|-----------|
| `baseUrl` | Base da API (`http://localhost:5166`) |
| `adminEmail` / `adminPassword` | Credenciais seed do admin (`admin@gatums.com` / `Admin@123`) |
| `adminToken` | Preenchido automaticamente ao rodar *Login (Admin)* |
| `clienteEmail` / `clientePassword` | Credenciais do cliente de teste (ajuste conforme o cadastro) |
| `clienteToken` | Preenchido automaticamente ao rodar *Login (Cliente)* |
| `produtoId` | Preenchido ao rodar *Criar Produto* |
| `clienteId` | Preenchido ao rodar *Criar Cliente* |
| `pedidoId` | Preenchido ao rodar *Checkout* |
| `transacaoId` | Preenchido ao rodar *Iniciar Pagamento* |

## Fluxo recomendado de testes

Os scripts de teste encadeiam as variáveis automaticamente. Siga esta ordem:

### Fluxo cliente (compra)

1. **Auth → Registrar (Cliente)** — cria um usuário cliente (se ainda não existir)
2. **Auth → Login (Cliente)** — captura `clienteToken`
3. **Produtos → Listar Produtos** — copie um `id` de produto e cole na variável `produtoId`
4. **Carrinho → Adicionar Item** — adiciona o produto ao carrinho
5. **Carrinho → Ver Carrinho** — confere itens e total
6. **Pedidos → Checkout** — cria o pedido (captura `pedidoId`)
7. **Pagamentos → Iniciar Pagamento** — inicia a transação (captura `transacaoId`)
8. **Pagamentos → Confirmar Pix** *ou* **Processar Cartão** — confirma o pagamento
9. **Pagamentos → Consultar Status** — verifica `aprovado`
10. **Pedidos → Concluir Pagamento** — vincula a transação ao pedido

### Fluxo admin (gestão)

1. **Auth → Login (Admin)** — captura `adminToken`
2. **Produtos → Criar Produto** — cadastra produto (captura `produtoId`)
3. **Produtos → Listar / Atualizar / Excluir** — gerencia catálogo
4. **Clientes → Criar / Listar / Atualizar / Excluir** — gerencia CRM
5. **Pedidos → Listar Pedidos** — vê todos os pedidos
6. **Pedidos → Avançar Etapa** — move o pedido na timeline (`proximaEtapa` como int)
7. **Pagamentos → Validar Pagamento** — valida transação (antes de `PagamentoAprovado`)
8. **Relatórios → Resumo de Vendas** — dashboard de vendas

## Autenticação

- JWT Bearer, header `Authorization: Bearer <token>`.
- Token válido por **8 horas**.
- Claims: `sub` (userId), `email`, `role` (`admin` ou `cliente`).
- Os requests protegidos já têm o auth configurado por pasta: Admin usa `{{adminToken}}`, Cliente usa `{{clienteToken}}`.

## Atenção — enums no body JSON

O projeto **não** registra `JsonStringEnumConverter`. Portanto:

- **Corpo JSON**: enviar enums como **inteiros**.
  - `MetodoPagamento`: `0` = CartaoDebito, `1` = CartaoCredito, `2` = Pix
  - `TipoProduto`: `0` = Simples, `1` = Combo
  - `PedidoEtapa`: `0` = Criado, `1` = Aprovado, `2` = Pagamento, `3` = PagamentoAprovado, `4` = EnvioPendente, `5` = Entregue, `6` = Finalizado
  - `EntregaTipo`: `0` = Transporte, `1` = Retirada
- **Query params**: aceitam o **nome** da string (case-insensitive), ex.: `?tipo=simples`.

> Se o front-end (Lovable) envia enums como string no body, a integração vai falhar. Há duas opções: (a) registrar `JsonStringEnumConverter` na API, ou (b) enviar inteiros no front. Veja `docs/Site/Erros-e-Padroes.md`.

## Inconsistência de formato do `metodo` de pagamento

| Onde | Formato | Exemplo |
|------|---------|---------|
| `GET /pagamentos/opcoes` (response) | string com underscore | `"cartao_credito"` |
| `POST /pagamentos/iniciar` (request body) | inteiro (enum) | `2` |
| `TransacaoResponse.metodo` (response) | string lowercase sem underscore | `"cartaocredito"` |

O mapeamento no front precisa tratar essas três representações.

## Pré-requisitos

- API rodando em `http://localhost:5166` (`dotnet run --project src/GatumsGourmet.Api`).
- PostgreSQL disponível (ver `docker/docker-compose.yml`). O `DbSeeder` cria o admin e produtos demo automaticamente.
- Swagger UI em `http://localhost:5166/swagger` (apenas em Development).
