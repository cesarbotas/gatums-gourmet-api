# Guia de uso — API de autenticação (para o front)

Este guia explica como integrar o front-end com a autenticação da API Gatums
Gourmet: login, uso do access token, renovação com refresh, tratamento de 401,
logout e a diferença entre roles e permissões.

Base URL: `{API_BASE}/api/v1`.

## Mudança de contrato (importante)

O contrato de login **mudou**. Antes era um token único:

```json
// ANTES (não vale mais)
{ "token": "...", "user": { "id": "...", "nome": "...", "email": "...", "role": "admin" } }
```

Agora o login devolve **access token + refresh token** e `user.roles` é uma
**lista**:

```json
// AGORA
{
  "accessToken": "eyJ...",
  "refreshToken": "9f8a...base64url",
  "expiresIn": 900,
  "user": { "id": "guid", "nome": "...", "email": "...", "roles": ["Admin"] }
}
```

- `accessToken`: JWT curto (~15 min). Vai no header `Authorization` das chamadas.
- `refreshToken`: valor opaco (~7 dias). Serve só para obter um novo access.
- `expiresIn`: validade do access **em segundos** (ex.: `900` = 15 min).
- `user.roles`: lista de nomes de role (informativo para a UI).

## Fluxo básico

### 1. Login

```http
POST /api/v1/auth/login
Content-Type: application/json

{ "email": "admin@gatums.com", "senha": "suaSenha" }
```

Resposta `200`: o objeto `LoginResponse` acima.

Guarde `accessToken` e `refreshToken` no cliente. O access é usado em toda
requisição autenticada; o refresh fica reservado para renovação.

> Usuário inativo não loga (`400 "Usuário inativo."`). Credenciais erradas →
> `400 "Credenciais inválidas."`.

### 2. Chamar rotas autenticadas

Envie o access token no header `Authorization`:

```http
GET /api/v1/auth/me
Authorization: Bearer eyJ...
```

`GET /auth/me` devolve o usuário atual:

```json
{ "id": "guid", "nome": "...", "email": "...", "roles": ["Admin"] }
```

### 3. Renovar quando o access expira (refresh)

O access vive ~15 min. Quando expirar (ou de forma proativa), troque o refresh
por um par novo:

```http
POST /api/v1/auth/refresh
Content-Type: application/json

{ "refreshToken": "9f8a...base64url" }
```

Resposta `200`: **mesmo shape do login** (`accessToken`, `refreshToken`,
`expiresIn`, `user`).

> **Rotação:** o refresh é de uso único. Cada `/refresh` **invalida** o refresh
> antigo e devolve um novo. Sempre substitua o refresh guardado pelo que veio na
> resposta.
>
> **Detecção de reúso:** se você reapresentar um refresh já usado/revogado, a
> API revoga **toda a família** de tokens daquele login e falha
> (`400 "Refresh token inválido."`). Nesse caso, faça login de novo.

### 4. Tratar 401

O `401 Unauthorized` vem quando não há token válido (access ausente, inválido ou
expirado). Estratégia recomendada no front:

1. Recebeu `401` numa chamada → tente `POST /auth/refresh` com o refresh atual.
2. Refresh **deu certo** → atualize o par de tokens e **repita** a chamada original.
3. Refresh **falhou** (`400`/refresh inválido ou expirado) → limpe os tokens e
   mande o usuário **relogar**.

> O `403 Forbidden` é diferente de `401`: o usuário está autenticado, mas não tem
> a **permissão** exigida pela rota. Não adianta dar refresh se a role dele não
> concede aquela permissão — a ação simplesmente não é permitida. (Se a permissão
> foi concedida há pouco, um refresh traz o token atualizado; ver seção
> "Roles vs permissões".)

### 5. Logout e logout de todas as sessões

```http
POST /api/v1/auth/logout
Authorization: Bearer <access>
Content-Type: application/json

{ "refreshToken": "9f8a..." }
```

Revoga a família do refresh informado (encerra aquela sessão).

```http
POST /api/v1/auth/logout-all
Authorization: Bearer <access>
```

Revoga **todos** os refresh ativos do usuário (útil após troca de senha).

> Observação: o access token em curso continua válido até expirar (~15 min),
> mesmo após o logout — é o comportamento esperado de JWT stateless. O logout
> impede **renovar**; descarte o access no cliente ao sair.

### Registro público

```http
POST /api/v1/auth/register
Content-Type: application/json

{ "nome": "...", "email": "...", "senha": "...", "telefone": "11999999999" }
```

Cria sempre um usuário **Cliente**. `telefone` é opcional. Para criar usuários
com outras roles, use a rota administrativa `POST /usuarios`
(exige `usuarios:gerenciar`).

## Roles vs permissões

- **Role** (ex.: `Admin`, `Gerente`, `Cliente`) é um rótulo que agrupa
  permissões. `user.roles` é informativo — útil para a UI decidir o que mostrar.
- **Permissão** (ex.: `pedidos:avancar`) é o que de fato libera uma operação
  administrativa. A API decide o acesso pelas permissões embutidas no access
  token, **não** pelo nome da role.

Consequência prática: se um admin conceder uma permissão nova à role do usuário,
ela **só vale no próximo access token**. O usuário precisa fazer `/refresh` (ou
relogar) para o token passar a carregar a permissão. Enquanto usar o access
antigo (até ~15 min), a rota vai responder `403`.

## Resumo dos endpoints de auth

| Método | Rota | Auth | Corpo |
|---|---|---|---|
| POST | `/auth/login` | público | `{ email, senha }` |
| POST | `/auth/register` | público | `{ nome, email, senha, telefone? }` |
| POST | `/auth/refresh` | público | `{ refreshToken }` |
| GET | `/auth/me` | Bearer | — |
| POST | `/auth/logout` | Bearer | `{ refreshToken }` |
| POST | `/auth/logout-all` | Bearer | — |
