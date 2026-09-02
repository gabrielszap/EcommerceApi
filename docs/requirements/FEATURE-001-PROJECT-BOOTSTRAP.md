# FEATURE-001 - Project Bootstrap, SQLite, and Delivery Foundation

## Activity and Git context

- Classification: new activity.
- Suggested branch: `infra/project-bootstrap`.
- Base branch: `develop`.
- Pull Request target: `develop`.
- Depends on: none.

This feature does not authorize commit, push, merge, rebase, or Pull Request creation.

## Goal

Create a small, runnable .NET 10 foundation for the order-management API that enforces Clean Architecture boundaries, persists orders in SQLite through EF Core, applies migrations automatically, and can run locally or through Docker Compose.

## Included

- Solution and project structure for Domain, Application, Infrastructure, API, and test projects.
- Dependency direction that keeps Domain and Application free of ASP.NET Core, EF Core, SQLite, and JWT implementation dependencies.
- Registration of MediatR, FluentValidation validation pipeline, EF Core SQLite, JWT bearer authentication, exception-to-Problem-Details handling, OpenAPI, and dependency injection.
- A focused Infrastructure `DbContext`, EF Core mapping/configuration for `Order` and `OrderItem`, and an initial committed migration.
- Startup execution of `Database.Migrate()` before the API accepts requests.
- Configuration of the SQLite connection string, including a documented safe local default.
- Dockerfile and `docker-compose.yml` for the API. If the SQLite file is created inside the container, configure a named volume or document the chosen bind mount.
- Initial README sections for prerequisites, local execution, Docker execution, database reset, migration behavior, and the chosen Minimal API versus Controllers rationale.
- A compilable test project and initial architecture/startup tests when practical.

## Excluded

- Order business endpoints and their use cases.
- A persisted User entity, registration, user seed, or credential migration.
- PostgreSQL, Dapper, Npgsql, a database container, or a message broker.
- Optional Serilog, OpenTelemetry, SonarQube, and endpoint integration tests unless the mandatory foundation is already complete.

## Architectural decisions

The Architect must choose Minimal APIs or Controllers and document a concise rationale in the README. For this small five-endpoint API, either approach is acceptable when the API layer remains thin and delegates to MediatR.

EF Core is permitted only in Infrastructure. `DbContext`, entity configurations, migrations, and transaction management remain there. Domain entities protect their invariants and do not expose public setters that allow invalid state.

## Persistence and migration contract

1. Store `Order` and `OrderItem` using SQLite and EF Core.
2. Map the relationship so one order owns many items and every item references its order.
3. Persist the required IDs, customer ID, status, created time, product name, quantity, and unit price.
4. Commit an initial EF Core migration to source control.
5. Execute `Database.Migrate()` at application startup; never call `EnsureCreated`.
6. If `Database.Migrate()` fails, log the exception and abort application startup.
7. Repeated startup must leave the database usable and must not duplicate schema changes.
8. The connection string and database file location must be configurable and documented for both local and Docker execution.

## Acceptance criteria

- Given a clean checkout, when the solution is restored and built, then every project compiles.
- Given a new or empty SQLite database, when the API starts, then the EF Core migration is applied before requests are served.
- Given a database that already has the current migration, when the API restarts, then startup succeeds without changing existing data unexpectedly.
- Given the Docker configuration, when `docker compose build` and `docker compose up` run, then the API starts using the configured SQLite location.
- Given the repository, when a reviewer inspects references, then Domain and Application have no dependency on EF Core, SQLite, or ASP.NET Core.
- Given the README, when a developer follows it, then they can run the API locally and through Docker and reset the SQLite database intentionally.

## Verification matrix

- Build: `dotnet restore`, `dotnet build --no-restore`.
- Tests: `dotnet test --no-build`.
- Docker: `docker compose config`, `docker compose build`, and observed API startup.
- Persistence: inspect the migration and verify migration application against a fresh temporary SQLite file.
- Review: inspect package references and project references for Clean Architecture dependency direction.

## Completion evidence

Report the branch, structure created, migration name, SQLite file/location behavior, Docker behavior, README commands, exact verification output, and any decisions deferred to later feature specs.
