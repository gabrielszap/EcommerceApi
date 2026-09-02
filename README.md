# EcommerceApi

Order Management practical-test API. This repository includes the runnable foundation and FEATURE-002 authentication; order use cases and routes are intentionally deferred to FEATURE-003 and later activities.

## Prerequisites

- .NET SDK 10.0.203 (or a compatible .NET 10 SDK)
- Docker Engine with Docker Compose v2 (for container execution)

Set `Jwt__SigningKey` to a development-only value of at least 32 UTF-8 bytes. It is required at startup, is never committed, and must be supplied through environment-specific configuration or a secret store in real deployments.

## Architecture

The solution uses Clean Architecture with four production projects:

- `EcommerceApi.Domain`: `Order` aggregate, `OrderItem`, status, invariants, and `TotalAmount` calculation.
- `EcommerceApi.Application`: MediatR registration and the FluentValidation pipeline behavior. Use cases will be added by later activities.
- `EcommerceApi.Infrastructure`: EF Core `OrderDbContext`, SQLite mappings, and migrations.
- `EcommerceApi.Api`: Minimal API host, JWT bearer registration, Problem Details, OpenAPI, dependency injection, and startup migration.

Minimal APIs were chosen because the final contract has five focused routes. Route groups and thin delegates keep the transport concise, while MediatR will execute use cases and the Domain will protect business invariants. Controllers would add ceremony without a current need for MVC filters or custom formatters.

The dependency direction is `Domain <- Application <- Api` and `Domain <- Infrastructure <- Api`. Domain and Application do not reference ASP.NET Core, EF Core, SQLite, or JWT implementation packages. There is no generic repository: persistence is kept in the focused `OrderDbContext` until a concrete use-case port is required.

## Local execution

From the repository root:

```powershell
$env:Jwt__SigningKey = 'local-development-key-with-at-least-32-bytes'
dotnet restore
dotnet run --project src/EcommerceApi.Api/EcommerceApi.Api.csproj
```

The API listens on the configured ASP.NET Core URL. OpenAPI is available at `/openapi/v1.json`. The currently exposed business endpoint is `POST /auth/login`; order routes are introduced by FEATURE-003.

The local default is `ConnectionStrings:Orders=Data Source=data/ecommerce.db` (and `data/ecommerce.development.db` in Development). The relative SQLite path is resolved from the process content root. The `data` directory is intentionally not committed.

## Docker Compose

Set a temporary development-only key and start the API:

```powershell
$env:JWT_SIGNING_KEY = 'local-development-key-with-at-least-32-bytes'
docker compose build
docker compose up
```

Compose starts only the API. It stores SQLite at `/app/data/ecommerce.db` in the named volume `ecommerceapi-data`, so container recreation preserves the database. The host port is `8080`.

To intentionally reset the Docker database, stop the stack and remove the named volume:

```powershell
docker compose down -v
```

This permanently deletes the named-volume database. For a local reset, stop the API and remove the selected `data/ecommerce*.db` file intentionally.

## Migrations and startup

The committed migration is `20260902145400_InitialOrderSchema`. It creates `Orders` and `OrderItems`, their required relationship/index/check constraints, and no `TotalAmount` column. `Database.MigrateAsync()` runs before `app.Run()`; a migration failure is logged at Critical and rethrown so the process does not serve requests. Repeated startup sees the existing migration history and leaves the schema/data intact. `EnsureCreated` is not used.

The migration source and model snapshot are versioned under `src/EcommerceApi.Infrastructure/Persistence/Migrations`.

The API host validates JWT issuer, audience, lifetime, signature, and a signing key of at least 32 bytes. The signing key is supplied through configuration/environment only and is never committed or logged.

## Authentication

`POST /auth/login` is anonymous and forwards its request to the Application layer through MediatR. FluentValidation runs in the MediatR pipeline, so the endpoint neither compares credentials nor creates a token.

| Outcome | Status | Body |
| --- | --- | --- |
| Valid fixed credentials | `200 OK` | `{ "accessToken": "...", "expiresAtUtc": "..." }` |
| Missing or invalid email/password shape | `400 Bad Request` | Validation Problem Details with field errors |
| Incorrect email or password | `401 Unauthorized` | Problem Details, without a token |

For the evaluator only, the fixed in-memory credentials are `dev@martech.com` / `Senha@123`. They are deliberately not persisted in SQLite or modeled as a user account. This is a test fixture and is not a production identity-management design.

Use the received token with routes protected in FEATURE-003 and later:

```http
Authorization: Bearer <accessToken>
```

The OpenAPI document describes the login request/response, validation and authentication errors, and the `Bearer` JWT security scheme.

### JWT configuration

| Key | Purpose |
| --- | --- |
| `Jwt__Issuer` | Issuer that this API creates and accepts. |
| `Jwt__Audience` | Audience that this API creates and accepts. |
| `Jwt__LifetimeMinutes` | Positive lifetime, in minutes, for access tokens. |
| `Jwt__SigningKey` | Secret symmetric key with at least 32 UTF-8 bytes; supply outside source control. |

`appsettings.json` contains safe issuer, audience, and lifetime defaults. It intentionally has no signing key. Environment variables use double underscores for nested configuration.

## Tests and quality checks

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
docker compose config
docker compose build
```

The tests use xUnit and a real temporary SQLite database for migration behavior; they do not use EF Core InMemory. Domain, MediatR validation pipeline, login handler, JWT signing/validation, migration, startup migration, and dependency behavior are covered in `tests/EcommerceApi.Tests`.

## Limitations and assumptions

- FEATURE-002 intentionally adds no order HTTP route. FEATURE-003 will add `/api/orders` and apply the already-configured JWT bearer authorization to it.
- There is no persisted user, registration, password reset, refresh token, OAuth flow, or optional observability.
- Startup migration is intentionally simple for this single-process practical test and is not a multi-instance migration coordinator.
- SQLite stores `decimal` values using its provider representation; monetary total calculation remains exclusively Domain behavior.
