# Gatums Gourmet API

Backend REST para o e-commerce Gatums Gourmet — .NET 10, Clean Architecture, PostgreSQL.

## Stack

- .NET 10 / ASP.NET Core Minimal APIs (rotas versionadas sob `/api/v1`)
- Clean Architecture (sem MediatR — handlers explícitos por Use Case)
- Entity Framework Core 10 + PostgreSQL (Npgsql)
- JWT Bearer + RBAC por permissão (access token curto + refresh token rotativo)
- Storage de imagens S3-compatível (MinIO local / Supabase hml / R2 prd) via `AWSSDK.S3`
- FluentValidation · Swagger/OpenAPI
- XUnit + Moq + FluentAssertions

## Início rápido (Docker)

```bash
# 1. Subir PostgreSQL + MinIO + API
docker compose -f docker/docker-compose.yml up -d

# 2. Swagger
open http://localhost:5080/swagger
```

> O `docker compose` agora sobe também o **MinIO** (storage de imagens local),
> com o bucket `gatums-imagens` já público. Console web em http://localhost:9001
> (`minioadmin`/`minioadmin`).

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
| Categorias | `/categorias` |
| Imagens | `/imagens` |
| Endereços (CEP) | `/enderecos` |
| Clientes (admin) | `/clientes` |
| Carrinho | `/carrinho` |
| Pedidos | `/pedidos` |
| Pagamentos | `/pagamentos` |
| Relatórios | `/relatorios` |
| Roles | `/roles` |
| Permissions | `/permissions` |
| Usuários | `/usuarios` |

Referência completa dos endpoints em [`docs/Site/API-Reference.md`](docs/Site/API-Reference.md).

## Evolução

Entregas recentes, em ordem lógica:

- **Versionamento das rotas (V1):** todos os endpoints passaram a viver sob
  `/api/v1`, abrindo caminho para uma futura V2 incremental.
- **RBAC + refresh token:** o login deixou de devolver um JWT único de longa
  duração. Agora são **access token curto + refresh token rotativo** (com
  detecção de reúso), e a autorização administrativa passou a ser por
  **permissão** (`recurso:acao`) em vez de role nominal. Ver
  [`docs/adr/`](docs/adr/) e [`docs/Auth/`](docs/Auth/).
- **Busca de CEP (ViaCEP):** endpoint público para autopreencher endereço pelo
  CEP. Ver [`docs/Site/Busca-CEP.md`](docs/Site/Busca-CEP.md).
- **Upload de imagens em storage S3-compatível:** endpoint `POST /imagens` que
  valida (JPEG/PNG/WebP por magic bytes, máx 5 MB) e grava num bucket, devolvendo
  a URL pública que o produto guarda em `fotoUrl`. Storage por ambiente:
  **MinIO** local, **Supabase Storage** em homologação, **Cloudflare R2** em
  produção. Ver [`docs/Site/Upload-Imagens.md`](docs/Site/Upload-Imagens.md).

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
