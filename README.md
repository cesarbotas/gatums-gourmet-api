# Gatums Gourmet API

Backend REST para o e-commerce Gatums Gourmet — .NET 8, Clean Architecture, PostgreSQL.

## Stack

- .NET 8 / ASP.NET Core Minimal APIs
- Clean Architecture (sem MediatR — handlers explícitos por Use Case)
- Entity Framework Core 8 + PostgreSQL (Npgsql)
- JWT Bearer (roles: `admin`, `cliente`)
- FluentValidation · Swagger/OpenAPI
- XUnit + Moq + FluentAssertions

## Início rápido (Docker)

```bash
# 1. Subir PostgreSQL + API
docker compose -f docker/docker-compose.yml up -d

# 2. Swagger
open http://localhost:5080/swagger
```

## Desenvolvimento local

```bash
# 1. Subir apenas o banco
docker compose -f docker/docker-compose.yml up postgres -d

# 2. Aplicar migrations + seed
dotnet ef database update \
  --project src/GatumsGourmet.Infrastructure \
  --startup-project src/GatumsGourmet.Api

# 3. Rodar a API
dotnet run --project src/GatumsGourmet.Api
# → http://localhost:5080/swagger
```

## Testes

```bash
dotnet test tests/GatumsGourmet.UnitTests
```

## Credenciais padrão (seed)

| Campo | Valor |
|-------|-------|
| Email | admin@gatums.com |
| Senha | Admin@123 |
| Role  | admin |

## Variáveis do front-end

```env
VITE_API_BASE_URL=http://localhost:5080/api/v1
VITE_PAYMENTS_MOCK=false
```

## Endpoints `/api/v1`

| Grupo | Prefixo |
|-------|---------|
| Auth | `/auth` |
| Produtos | `/produtos` |
| Clientes (admin) | `/clientes` |
| Carrinho | `/carrinho` |
| Pedidos | `/pedidos` |
| Pagamentos | `/pagamentos` |
| Relatórios | `/relatorios` |

## Estrutura

```
src/
  GatumsGourmet.Domain          # Entidades, enums, interfaces, exceções
  GatumsGourmet.Application     # Use Cases (1 handler por endpoint), Result<T>
  GatumsGourmet.Infrastructure  # EF Core, repositórios, JWT, BCrypt, mocks PSP
  GatumsGourmet.Api             # Minimal APIs, middleware, DI wiring

tests/
  GatumsGourmet.UnitTests       # Handlers, workflow, validators

docker/
  Dockerfile
  docker-compose.yml
```

## Pagamentos em produção

Substituir `MockPaymentGateway` e `MockPixProvider` em `InfrastructureServiceCollectionExtensions` pelas implementações reais (Stripe / PagSeguro). As interfaces `IPaymentGateway` e `IPixProvider` estão em `Application/Interfaces/IExternalServices.cs`.
