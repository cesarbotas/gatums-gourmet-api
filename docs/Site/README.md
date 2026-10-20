# Gatums Gourmet — Documentação de Integração Site ↔ API

Documentação de referência para integrar o site (desenvolvido parcialmente no Lovable) com a API Gatums Gourmet (.NET 10 + PostgreSQL).

## Visão geral

| Item | Valor |
|------|-------|
| **Base URL** | `http://localhost:5166/api/v1` (dev) · `http://<vps-ip>/api/v1` (prod) |
| **Autenticação** | JWT Bearer (`Authorization: Bearer <token>`) |
| **Roles** | `admin` · `cliente` |
| **Token lifetime** | 8 horas |
| **CORS** | Liberado para qualquer origem (`AllowAnyOrigin`) |
| **JSON casing** | camelCase (`{ "nome": "...", "preco": 12.90 }`) |
| **Swagger** | `http://localhost:5166/swagger` (apenas Development) |
| **Content-Type** | `application/json` |

## Documentos

| Documento | Descrição |
|-----------|-----------|
| [Autenticacao.md](Autenticacao.md) | Login, registro, token JWT, roles, claims |
| [API-Reference.md](API-Reference.md) | Todos os endpoints com método, rota, body e auth |
| [Fluxos-Integracao.md](Fluxos-Integracao.md) | Fluxos completos: catálogo, carrinho, checkout, pagamento, pedidos, admin |
| [Modelos-de-Dados.md](Modelos-de-Dados.md) | Todos os DTOs com propriedades, tipos e exemplos |
| [Erros-e-Padroes.md](Erros-e-Padroes.md) | Tratamento de erros, ProblemDetails, enums, convenções |

## Variável de ambiente do site

```env
# .env do site (Vite/Lovable)
VITE_API_BASE_URL=http://localhost:5166/api/v1
```

Em produção, apontar para a VPS:
```env
VITE_API_BASE_URL=http://<vps-ip>/api/v1
```

## Credenciais de teste (seed)

A API cria automaticamente no primeiro startup (via `DbSeeder`):

| Usuário | Email | Senha | Role |
|---------|-------|-------|------|
| Admin | `admin@gatums.com` | `Admin@123` | `admin` |

Cliente não tem seed — registre via `POST /api/v1/auth/register` (sempre atribui role `cliente`).

## Resumo dos fluxos

```
Cliente:
  Register → Login → Listar Produtos → Adicionar ao Carrinho
    → Checkout → Iniciar Pagamento → Confirmar Pix/Cartão
    → Concluir Pagamento → Ver Pedidos

Admin:
  Login → Gerenciar Produtos → Gerenciar Clientes
    → Validar Pagamentos → Avançar Pedidos → Relatórios
```

## Avisos importantes para a integração

1. **Enums no body JSON**: sem `JsonStringEnumConverter`, enums em corpo JSON são **inteiros** (ver [Erros-e-Padroes.md](Erros-e-Padroes.md#enums)).
2. **Formato do `metodo` de pagamento é inconsistente** entre endpoints (ver [Erros-e-Padroes.md](Erros-e-Padroes.md#inconsistência-de-metodo-de-pagamento)).
3. **Cliente ≠ Usuário**: o CRUD de `/clientes` é um CRM admin, distinto do `/auth/register`.
4. **Validadores não são invocados**: FluentValidation está registrado mas nenhum handler chama `.Validate()`. Validação de campos deve ser feita no front.
