# Autenticação

A API usa JWT Bearer. O token é obtido via login ou registro e enviado no header `Authorization` de todas as rotas protegidas.

## Obter token

### Login

```
POST /api/v1/auth/login
```

**Body:**
```json
{
  "email": "admin@gatums.com",
  "senha": "Admin@123"
}
```

**Resposta 200:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR...",
  "user": {
    "id": "3f5e2a1b-...",
    "nome": "Admin",
    "email": "admin@gatums.com",
    "role": "admin"
  }
}
```

**Erro 400 (credenciais inválidas):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Credenciais inválidas."
}
```

### Registro

```
POST /api/v1/auth/register
```

**Body:**
```json
{
  "nome": "João Cliente",
  "email": "joao@exemplo.com",
  "senha": "Senha@123",
  "telefone": "11999999999"
}
```

> O campo `telefone` é opcional (pode ser `null`).

**Resposta 201:**
```json
{
  "id": "a1b2c3d4-...",
  "nome": "João Cliente",
  "email": "joao@exemplo.com",
  "role": "cliente"
}
```

> O registro **sempre** atribui role `cliente`. Não há como criar admin via API.

## Usar o token

Enviar em todas as rotas protegidas:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR...
```

## Token JWT — detalhes

| Claim | Valor | Descrição |
|-------|-------|-----------|
| `sub` | userId (Guid) | Identificador do usuário |
| `email` | email | Email do usuário |
| `role` | `"admin"` ou `"cliente"` | Role para autorização |
| `jti` | Guid aleatório | ID único do token |

| Propriedade | Valor |
|-------------|-------|
| Algoritmo | HS256 (HMAC-SHA256) |
| Issuer | `GatumsGourmet` |
| Audience | `GatumsGourmet.Web` |
| Expiração | 8 horas |
| Validação | Issuer + Audience + Lifetime + SigningKey |

> O token é **stateless** — não há revogação no servidor. O endpoint `POST /auth/logout` é um no-op (retorna 200 sem efeito).

## Políticas de autorização

| Política | Quem pode acessar |
|----------|-------------------|
| *(nenhuma)* | Qualquer um autenticado (token válido) |
| `AdminOnly` | Role `admin` |
| `ClienteOnly` | Role `cliente` |

- Sem token ou token inválido → **401 Unauthorized**
- Token válido mas role errada → **403 Forbidden**

## Endpoints de auth

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| POST | `/auth/login` | público | Autentica e retorna token |
| POST | `/auth/register` | público | Registra novo cliente |
| GET | `/auth/me` | autenticado | Retorna dados do usuário atual |
| POST | `/auth/logout` | autenticado | No-op (JWT é stateless) |

## Exemplo de uso no front (TypeScript)

```typescript
const API_BASE = import.meta.env.VITE_API_BASE_URL; // http://localhost:5166/api/v1

// Login
const res = await fetch(`${API_BASE}/auth/login`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, senha }),
});
const { token, user } = await res.json();

// Salvar token (ex: localStorage ou estado)
localStorage.setItem('gatums_token', token);

// Requisição autenticada
const res = await fetch(`${API_BASE}/carrinho`, {
  headers: { Authorization: `Bearer ${localStorage.getItem('gatums_token')}` },
});
```

## Recomendações para o front

1. **Armazenar token**: preferir `localStorage` ou contexto de estado. Limpar no logout e na expiração (401).
2. **Interceptor**: adicionar `Authorization: Bearer` automaticamente em todas as requests autenticadas.
3. **Refresh**: não há refresh token. Após 8h, o usuário precisa logar novamente. Tratar o 401 redirecionando para o login.
4. **Validação de campos**: a API não valida inputs (FluentValidation está registrado mas não é invocado). Validar email, senha, etc. no front.
