# API Overview - Order Management Practical Test

## Purpose

Implement the backend of a simple e-commerce order-management system. The evaluator prioritizes architectural quality, clear and testable code, and decisions that can be explained during a live interview over feature quantity.

## Mandatory constraints

- .NET 10 and ASP.NET Core.
- Clean Architecture with Domain, Application, Infrastructure, and API projects.
- CQRS with MediatR; commands and queries are separate.
- Entity Framework Core with SQLite.
- EF Core migrations are committed and applied automatically during startup with `Database.Migrate()`; `EnsureCreated` is prohibited.
- JWT authentication with the in-memory test credentials `dev@martech.com` / `Senha@123`.
- FluentValidation runs through a MediatR pipeline behavior.
- xUnit unit tests cover every command and query handler.
- Dockerfile, `docker-compose.yml`, and a README with local and Docker instructions.

The repository must not add PostgreSQL, Dapper, Npgsql, a message broker, a persisted user store, registration, or an EF Core in-memory provider.

## Domain

`Order` is the aggregate and consistency boundary.

| Concept | Required fields and behavior |
| --- | --- |
| `Order` | `Id: Guid`, `CustomerId: Guid`, `Status: Pending | Confirmed | Cancelled`, `CreatedAt: DateTime`, and one or more `OrderItem`s. |
| `OrderItem` | `Id: Guid`, `OrderId: Guid`, `ProductName: string`, `Quantity: int`, and `UnitPrice: decimal`. |
| `TotalAmount` | Exposed in the DTO and populated through Domain behavior as the sum of `UnitPrice * Quantity` for every item; it is never recalculated as a business rule in API, Application, or Infrastructure. |

Business invariants:

1. An order contains at least one item.
2. `Quantity` and `UnitPrice` are greater than zero.
3. Only an order in `Pending` status can be cancelled.
4. New orders start in `Pending` status.
5. No endpoint is required to confirm an order; `Confirmed` remains part of the required enum only.

## API surface

All `/api/orders` endpoints require a valid JWT. `POST /auth/login` is anonymous.

| Method | Route | Outcome |
| --- | --- | --- |
| `POST` | `/auth/login` | Validates the fixed in-memory credentials and returns a JWT. |
| `POST` | `/api/orders` | Creates an order that satisfies the domain invariants. |
| `GET` | `/api/orders?page=1&pageSize=10` | Returns a paginated order list. |
| `GET` | `/api/orders/{id}` | Returns one order by identifier. |
| `PATCH` | `/api/orders/{id}/cancel` | Cancels an order only when it is pending. |

The concrete request and response DTOs, status codes, validation errors, and pagination envelope are defined in the feature specs below. API behavior must be exposed through OpenAPI and documented in the README.

## Authentication and security

The fixed pair `dev@martech.com` / `Senha@123` is a test fixture and the only required authentication source. It is not persisted and does not imply a production credential-management design.

Login is implemented through a MediatR command. The endpoint must not validate credentials or construct a JWT directly. JWT issuer, audience, lifetime, and signing key come from configuration. The signing key, submitted password, and issued JWT must never be logged or committed as a real secret.

## Persistence, startup, and Docker

`DbContext`, entity configurations, migrations, transactions, and EF Core access reside in Infrastructure. Domain and Application must not reference EF Core or SQLite types.

The application applies the initial EF Core migration for `Order` and `OrderItem` before serving requests. If `Database.Migrate()` fails, the error is logged and application startup is aborted. The SQLite file location comes from configuration. Docker Compose runs the API; when the database file is inside a container, use a named volume or document a bind mount so restart behavior is predictable. No database container is required.

## Development activities

Implement and review the following activities in order. Each is a new branch from `develop` unless it is clearly a continuation of the current activity.

1. `FEATURE-001-PROJECT-BOOTSTRAP.md`
2. `FEATURE-002-AUTHENTICATION-JWT.md`
3. `FEATURE-003-CREATE-ORDER.md`
4. `FEATURE-004-QUERY-ORDERS.md`
5. `FEATURE-005-CANCEL-ORDER.md`
6. `FEATURE-006-OPTIONAL-OBSERVABILITY.md` only after every mandatory requirement is stable.

## Completion standard

For each activity, the Orchestrator prepares the branch according to `AGENTS.md`, runs the architect/developer/reviewer/qa/platform workflow where applicable, and reports the final diff, test evidence, quality-gate evidence, and remaining risks. Commits, pushes, merges, rebases, and Pull Requests require explicit user authorization.

## Delivery and interview-readiness

The final deliverable is a private GitHub repository shared with the evaluator or a ZIP sent by email, within the agreed deadline. The Orchestrator may prepare the repository or ZIP but must not share access or send email without explicit user authorization.

The README must explain the architecture, the Minimal API versus Controllers choice, local and Docker execution, SQLite/migrations, fixed test credentials, JWT use, endpoint contracts, tests, completed optional items, limitations, and assumptions. Keep every design small enough to explain and extend live during the interview.
