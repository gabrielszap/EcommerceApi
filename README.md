# EcommerceApi

Order Management practical-test API. This repository includes the runnable foundation, FEATURE-002 authentication, FEATURE-003 order creation, FEATURE-004 order queries, and FEATURE-005 order cancellation. Confirmation endpoints, payment, catalog, stock, customer, discount, and idempotency flows are intentionally deferred to later activities or excluded by the feature scope.

## Prerequisites

- .NET SDK 10.0.203 (or a compatible .NET 10 SDK)
- Docker Engine with Docker Compose v2 (for container execution)

Set `Jwt__SigningKey` to a development-only value of at least 32 UTF-8 bytes. It is required at startup, is never committed, and must be supplied through environment-specific configuration or a secret store in real deployments.

## Architecture

The solution uses Clean Architecture with four production projects:

- `EcommerceApi.Domain`: `Order` aggregate, `OrderItem`, status, invariants, cancellation transition, and `TotalAmount` calculation.
- `EcommerceApi.Application`: MediatR registration, the FluentValidation pipeline behavior, fixed-credential login, create-order command/handler, cancel-order command/handler, query handlers, and application-owned order persistence ports.
- `EcommerceApi.Infrastructure`: EF Core `OrderDbContext`, SQLite mappings, migrations, JWT generation, the EF order writer, and EF order read queries.
- `EcommerceApi.Api`: Minimal API host, JWT bearer registration, Problem Details, OpenAPI, dependency injection, startup migration, authentication endpoint, and protected order endpoints.

Minimal APIs were chosen because the final contract has five focused routes. Route groups and thin delegates keep the transport concise, while MediatR will execute use cases and the Domain will protect business invariants. Controllers would add ceremony without a current need for MVC filters or custom formatters.

The dependency direction is `Domain <- Application <- Api` and `Domain <- Application <- Infrastructure <- Api`. Domain and Application do not reference ASP.NET Core, EF Core, SQLite, or JWT implementation packages. There is no generic repository: order use cases expose focused `IOrderWriter` and `IOrderReader` ports.

## Local execution

From the repository root:

```powershell
$env:Jwt__SigningKey = 'local-development-key-with-at-least-32-bytes'
dotnet restore
dotnet run --project src/EcommerceApi.Api/EcommerceApi.Api.csproj
```

The API listens on the configured ASP.NET Core URL. OpenAPI is available at `/openapi/v1.json`. The currently exposed endpoints are `POST /auth/login`, protected `POST /api/orders`, protected `GET /api/orders`, protected `GET /api/orders/{id}`, and protected `PATCH /api/orders/{id}/cancel`.

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

The committed migration is `20260902145400_InitialOrderSchema`. It creates `Orders` and `OrderItems`, their required relationship/index/check constraints, and no `TotalAmount` column. FEATURE-003 did not require a new migration because this schema already persists the required order and item fields. `Database.MigrateAsync()` runs before `app.Run()`; a migration failure is logged at Critical and rethrown so the process does not serve requests. Repeated startup sees the existing migration history and leaves the schema/data intact. `EnsureCreated` is not used.

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

Use the received token with protected routes:

```http
Authorization: Bearer <accessToken>
```

The OpenAPI document describes the login request/response, validation and authentication errors, and the `Bearer` JWT security scheme.

## Create order

`POST /api/orders` requires `Authorization: Bearer <accessToken>`. The endpoint forwards the request to MediatR, FluentValidation validates the request shape in the pipeline, the Domain constructs the `Order` aggregate and items, and Infrastructure persists the aggregate through EF Core/SQLite in one `SaveChangesAsync()` call.

Request:

```json
{
  "customerId": "11111111-1111-1111-1111-111111111111",
  "items": [
    {
      "productName": "Keyboard",
      "quantity": 2,
      "unitPrice": 150.00
    }
  ]
}
```

Successful response: `201 Created`, `Location: /api/orders/{id}`, and a body containing `id`, `customerId`, `status`, `createdAt`, `items`, and Domain-calculated `totalAmount`.

Validation and domain-rule failures return `400 Bad Request` Problem Details. Missing, malformed, invalid, or expired bearer tokens return `401 Unauthorized`. Product catalog lookup, stock validation, price lookup, payment, confirmation, customer persistence, discounts, and idempotency keys are not implemented.

## Query orders

Both read endpoints require `Authorization: Bearer <accessToken>`. They are implemented as MediatR queries and do not expose EF Core entities or queryables outside Infrastructure.

`GET /api/orders?page=1&pageSize=10` returns `200 OK` with this pagination envelope:

```json
{
  "items": [
    {
      "id": "22222222-2222-2222-2222-222222222222",
      "customerId": "11111111-1111-1111-1111-111111111111",
      "status": "Pending",
      "createdAt": "2026-09-02T12:00:00Z",
      "itemCount": 2,
      "totalAmount": 300.00
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

`page` defaults to `1`, `pageSize` defaults to `10`, and both must be positive integers. `pageSize` is capped at `100`. Invalid pagination returns `400 Bad Request` Validation Problem Details. Results are ordered by newest `createdAt` first, with `id` as the stable tie-breaker, and pagination is applied by EF Core at the database query level.

`GET /api/orders/{id}` returns `200 OK` with `id`, `customerId`, `status`, `createdAt`, `items`, and Domain-calculated `totalAmount`. A malformed GUID or empty GUID returns `400 Bad Request` Problem Details. A well-formed but unknown GUID returns `404 Not Found` Problem Details.

## Cancel order

`PATCH /api/orders/{id}/cancel` requires `Authorization: Bearer <accessToken>` and has no request body. The endpoint parses the route identifier and sends a MediatR command. The handler loads the tracked aggregate through the Application-owned write port, calls `Order.Cancel()`, and persists the changed status with EF Core in the same context. `Status` is configured as an EF Core concurrency token, so a stale cancellation save is translated to the same `409 Conflict` invalid-state outcome instead of reporting a second success.

Successful response: `200 OK` with the order representation, including `status: "Cancelled"` and Domain-calculated `totalAmount`.

| Outcome | Status | Body |
| --- | --- | --- |
| Existing pending order | `200 OK` | Cancelled order representation |
| Malformed GUID | `400 Bad Request` | Problem Details |
| Unknown valid GUID | `404 Not Found` | Problem Details |
| Already cancelled or confirmed order | `409 Conflict` | Problem Details |
| Missing, malformed, invalid, or expired bearer token | `401 Unauthorized` | Problem Details |

No endpoint currently confirms orders. The Domain exposes a confirmation transition only so the aggregate can represent and protect the `Confirmed` state required by the order lifecycle; cancellation still only permits `Pending -> Cancelled`.

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

The tests use xUnit and a real temporary SQLite database for migration and persistence behavior; they do not use EF Core InMemory. Domain, MediatR validation/logging pipelines, login handler, create-order handler, cancel-order handler, query handlers, JWT signing/validation, migration, startup migration, EF order persistence/read/cancellation behavior, and order API behavior are covered in `tests/EcommerceApi.Tests`.

## Optional observability

FEATURE-006 implements the first optional hardening item only: a MediatR logging pipeline behavior backed by Serilog in the API host. The behavior logs request type, outcome, and elapsed duration for commands and queries. It does not log request payloads, submitted passwords, JWT values, signing keys, or full command bodies.

Serilog is configured during host startup and writes structured logs to the console. The Application layer still depends only on `Microsoft.Extensions.Logging.Abstractions`; Serilog remains an API-host logging provider rather than a business dependency.

The repository already includes API integration tests through `WebApplicationFactory`, satisfying the second optional item without adding new infrastructure. OpenTelemetry console export and SonarQube tooling were intentionally deferred because they are lower-priority optional items and would add extra dependencies or workflow assumptions beyond the completed mandatory API.

## Limitations and assumptions

- Order creation, read, and cancellation endpoints are implemented. Confirm/payment/catalog/stock/customer/discount/idempotency behavior is outside the current scope.
- There is no persisted user, registration, password reset, refresh token, or OAuth flow.
- Optional observability is limited to Serilog-backed MediatR request logging; no OpenTelemetry backend, dashboard, or SonarQube workflow is configured.
- Startup migration is intentionally simple for this single-process practical test and is not a multi-instance migration coordinator.
- SQLite stores `decimal` values using its provider representation; monetary total calculation remains exclusively Domain behavior.
