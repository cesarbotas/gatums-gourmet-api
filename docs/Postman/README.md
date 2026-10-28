# Postman — Gatums Gourmet API

Coleção e ambiente para testar a API Gatums Gourmet (.NET 10) com **RBAC + refresh token**.

## Arquivos

| Arquivo | Descrição |
|---------|-----------|
| `GatumsGourmet.postman_collection.json` | Coleção completa com scripts que capturam tokens e IDs automaticamente |
| `GatumsGourmet-Local.environment.json` | Ambiente local com variáveis de conexão e credenciais seed |

## Como importar

1. Abra o Postman.
2. **Import** → arraste os dois arquivos (ou *Upload Files*).
3. Selecione o ambiente **Gatums Gourmet - Local** no seletor no canto superior direito.

## Grupos da coleção

Auth · Produtos · Categorias · Endereços (CEP) · Clientes (CRM) · Carrinho · Pedidos ·
Pagamentos · Relatórios · Roles (RBAC) · Permissions (RBAC) · Usuários (RBAC).

## Variáveis

| Variável | Descrição |
|----------|-----------|
| `baseUrl` | Base da API (`http://localhost:5166`) |
| `adminEmail` / `adminPassword` | Credenciais seed do admin (`admin@gatums.com` / `Admin@123`) |
| `adminToken` / `adminRefreshToken` | Preenchidos ao rodar *Login (Admin)* |
| `clienteEmail` / `clientePassword` | Credenciais do cliente de teste |
| `clienteToken` / `clienteRefreshToken` | Preenchidos ao rodar *Login (Cliente)* |
| `clienteUserId` | Id do usuário cliente (do login) |
| `produtoId` / `categoriaId` | Preenchidos ao listar/criar produto/categoria |
| `clienteId` | Preenchido ao criar cliente (CRM) |
| `pedidoId` / `transacaoId` | Preenchidos no checkout / iniciar pagamento |
| `roleId` / `permissionId` / `usuarioId` | Preenchidos nos grupos de RBAC |

## Autenticação (RBAC + refresh token)

- **Login/refresh** devolvem `{ accessToken, refreshToken, expiresIn, user: { id, nome, email, roles: [] } }`.
  Os scripts salvam `accessToken` e `refreshToken` automaticamente.
- **Access token** é curto (default **15 min**). Renove com **Auth → Refresh Token** enviando
  `{ refreshToken }` — o refresh é **rotativo** (o antigo é invalidado). Reapresentar um refresh já
  rotacionado revoga a família inteira (detecção de reúso) e retorna 400.
- **Logout** envia `{ refreshToken }` no body (revoga a sessão). **Logout-all** revoga todas.
- Header: `Authorization: Bearer <accessToken>`. Cada pasta/requisição já aponta para
  `{{adminToken}}` ou `{{clienteToken}}`.
- Claims do access token: `sub`, `email`, uma claim `role` por role, uma claim `perm` por permissão, `jti`.

### Autorização por permissão (não mais por role nominal)

As rotas administrativas exigem **permissões** (ex.: `produtos:escrever`, `usuarios:gerenciar`),
não a antiga policy `AdminOnly`/`ClienteOnly`. O **Admin** do seed tem todas as 12 permissões,
então passa em qualquer rota. As roles de seed são **Admin**, **Cliente** e **Gerente** (PascalCase).

## Fluxo recomendado

### Cliente (compra)

1. **Auth → Registrar (Cliente)** → **Login (Cliente)** (captura tokens + `clienteUserId`)
2. **Categorias → Listar** (captura `categoriaId`) · **Produtos → Listar** (captura `produtoId`)
3. **Carrinho → Adicionar Item** → **Ver Carrinho**
4. **Pedidos → Checkout** (captura `pedidoId`)
5. **Pagamentos → Iniciar** (captura `transacaoId`) → **Confirmar Pix** *ou* **Processar Cartão** → **Consultar Status**
6. **Pedidos → Concluir Pagamento**

### Admin (gestão)

1. **Auth → Login (Admin)** (captura tokens)
2. **Categorias → Criar** → **Produtos → Criar** (usa `categoriaId`)
3. **Clientes (CRM)**, **Pedidos → Listar / Avançar Etapa**, **Pagamentos → Validar**, **Relatórios → Resumo**
4. **RBAC**: **Roles → Listar/Criar/Definir Permissões**, **Permissions → Listar/Criar**, **Usuários → Criar/Definir Roles/Ativar/Excluir**

### Endereço por CEP

**Endereços (CEP) → Buscar Endereço por CEP** — público, retorna o endereço para autopreencher
o formulário antes de **Clientes → Criar**.

## Enums no body JSON

O projeto **não** registra `JsonStringEnumConverter` — enviar enums como **inteiros** no corpo:

- `MetodoPagamento`: `0` = CartaoDebito, `1` = CartaoCredito, `2` = Pix
- `TipoProduto`: `0` = Simples, `1` = Combo
- `PedidoEtapa`: `0` = Criado, `1` = Aprovado, `2` = Pagamento, `3` = PagamentoAprovado, `4` = EnvioPendente, `5` = Entregue, `6` = Finalizado
- `EntregaTipo`: `0` = Transporte, `1` = Retirada

Query params aceitam o **nome** da string (case-insensitive), ex.: `?tipo=simples`.

## Paginação

As listagens usam query `pagina` / `tamanho` e respondem com envelope:
`{ itens: [], paginaAtual, tamanhoPagina, totalRegistros, totalPaginas, temPaginaAnterior, temProximaPagina }`.

## Pré-requisitos

- API rodando em `http://localhost:5166` (`dotnet run --project src/GatumsGourmet.Api`).
- PostgreSQL disponível (ver `docker/docker-compose.yml`). A migration cria as tabelas de RBAC e
  semeia as roles/permissões; o `DbSeeder` cria o admin (`admin@gatums.com` / `Admin@123`) vinculado à role Admin.
- OpenAPI/Scalar em Development.

> Observação: ajuste `baseUrl` se a API subir em outra porta (ex.: `5080` no Docker local).
