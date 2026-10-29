# Gatums Gourmet — Documentação de Integração Site ↔ API

Documentação de referência para integrar o site (desenvolvido parcialmente no Lovable) com a API Gatums Gourmet (.NET 10 + PostgreSQL).

## Visão geral

| Item | Valor |
|------|-------|
| **Base URL** | `http://localhost:5166/api/v1` (dev) · HML/PRD conforme deploy |
| **Autenticação** | JWT Bearer — **access token (15 min) + refresh token (7 dias)** |
| **Autorização** | Por **permissão** (RBAC); `user.roles` é um **array** (PascalCase) |
| **Token lifetime** | access 15 min · refresh 7 dias (renovar via `POST /auth/refresh`) |
| **CORS** | Restrito por ambiente (dev libera tudo; HML/PRD restringem) |
| **JSON casing** | camelCase (`{ "nome": "...", "preco": 12.90 }`) |
| **Swagger/Scalar** | `http://localhost:5166/scalar` (Development/Release) |
| **Content-Type** | `application/json` (exceto upload de imagem, que é `multipart/form-data`) |

> **Começando a integração ou atualizando o front?** Leia primeiro o
> **[Guia-Integracao-Front.md](Guia-Integracao-Front.md)** — consolida tudo o que
> mudou (auth RBAC+refresh, versionamento, paginação, CEP, upload de imagens, CORS)
> e traz um checklist de migração.

## Documentos

| Documento | Descrição |
|-----------|-----------|
| [Guia-Integracao-Front.md](Guia-Integracao-Front.md) | **Comece aqui.** O que mudou na API + checklist de migração |
| [Migracao-Front-Auth-RBAC.md](Migracao-Front-Auth-RBAC.md) | Auth nova (access+refresh, RBAC) com código pronto |
| [API-Reference.md](API-Reference.md) | Todos os endpoints com método, rota, body e auth |
| [Busca-CEP.md](Busca-CEP.md) | Autopreenchimento de endereço por CEP |
| [Upload-Imagens.md](Upload-Imagens.md) | Upload de imagem de produto |
| [Fluxos-Integracao.md](Fluxos-Integracao.md) | Fluxos completos: catálogo, carrinho, checkout, pagamento, pedidos, admin |
| [Modelos-de-Dados.md](Modelos-de-Dados.md) | Todos os DTOs com propriedades, tipos e exemplos |
| [Erros-e-Padroes.md](Erros-e-Padroes.md) | Tratamento de erros, ProblemDetails, enums, convenções |
| [Autenticacao.md](Autenticacao.md) | ⚠️ Desatualizado (descreve o JWT único antigo) — use o Guia e o de Migração |

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
