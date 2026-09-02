# Project Bootstrap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a runnable .NET 10 Clean Architecture foundation with a protected Order aggregate, EF Core SQLite persistence, an initial migration, automatic startup migration, structural authentication/Problem Details/OpenAPI registration, Docker Compose, and tests, without implementing future business endpoints.

**Architecture:** Minimal APIs will be the thin composition/delivery layer because the eventual contract has five focused routes and no MVC-specific requirements. Domain owns `Order`, `OrderItem`, `OrderStatus`, invariants, and `TotalAmount`; Application owns MediatR registration and validation pipeline; Infrastructure owns EF Core/SQLite mapping and migrations; API owns host configuration, JWT bearer wiring, exception handling, and startup migration orchestration.

**Tech Stack:** .NET 10, ASP.NET Core 10 Minimal APIs, MediatR, FluentValidation, EF Core 10 SQLite, JWT bearer authentication, built-in Problem Details/OpenAPI, xUnit.

**Spec:** `docs/requirements/FEATURE-001-PROJECT-BOOTSTRAP.md`

## Global Constraints

- Target `net10.0` and use ASP.NET Core 10.
- Keep Domain and Application free from ASP.NET Core, EF Core, SQLite, JWT implementation, and deployment dependencies.
- Use EF Core SQLite only; never add PostgreSQL, Dapper, Npgsql, a broker, `EnsureCreated`, or EF Core InMemory.
- Do not implement order endpoints, login, credentials, token generation, or optional observability in this activity.
- Apply the committed migration with `Database.Migrate()` before `app.Run()` and rethrow startup failures after critical logging.
- Do not add a generic `IRepository<T>` or speculative ports.
- Do not commit, push, merge, rebase, create a PR, or delete a branch.

---

### Task 1: Solution and dependency skeleton

**Files:**
- Create: `EcommerceApi.sln`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.gitignore`, `.dockerignore`
- Create: `src/EcommerceApi.Domain/EcommerceApi.Domain.csproj`
- Create: `src/EcommerceApi.Application/EcommerceApi.Application.csproj`
- Create: `src/EcommerceApi.Infrastructure/EcommerceApi.Infrastructure.csproj`
- Create: `src/EcommerceApi.Api/EcommerceApi.Api.csproj`
- Create: `tests/EcommerceApi.Tests/EcommerceApi.Tests.csproj`

**Interfaces:**
- Produces project names/namespaces `EcommerceApi.Domain`, `EcommerceApi.Application`, `EcommerceApi.Infrastructure`, `EcommerceApi.Api` and test references used by later tasks.
- Project references must be `Application -> Domain`, `Infrastructure -> Domain`, `Api -> Application + Infrastructure`, and tests -> required production projects.

- [ ] **Step 1: Create the projects with the .NET 10 templates and add them to the solution.**
- [ ] **Step 2: Add only required package references using central package management.**
- [ ] **Step 3: Add a marker type in Domain/Application so assembly scanning has stable assemblies.**
- [ ] **Step 4: Run `dotnet restore` and inspect project references with `dotnet list ... reference`.**

### Task 2: Domain behavior, test-first

**Files:**
- Create: `tests/EcommerceApi.Tests/Domain/OrderTests.cs`
- Create: `src/EcommerceApi.Domain/Common/DomainRuleViolationException.cs`
- Create: `src/EcommerceApi.Domain/Orders/OrderStatus.cs`
- Create: `src/EcommerceApi.Domain/Orders/OrderItem.cs`
- Create: `src/EcommerceApi.Domain/Orders/Order.cs`

**Interfaces:**
- `OrderItem.Create(string productName, int quantity, decimal unitPrice)` returns a valid item or throws `DomainRuleViolationException` for non-positive quantity/price.
- `Order.Create(Guid customerId, IEnumerable<OrderItem> items, DateTime createdAt)` returns a Pending order or throws for an empty collection.
- `Order.Items` is read-only; `Order.TotalAmount` sums `UnitPrice * Quantity`; no `TotalAmount` persistence member is required.
- `Order` has no public status setter; cancellation behavior is intentionally deferred to FEATURE-005.

- [ ] **Step 1: Write tests for valid creation/status, total calculation, empty items, zero/negative quantity, and zero/negative price.**
- [ ] **Step 2: Run `dotnet test tests/EcommerceApi.Tests --filter FullyQualifiedName~Domain` and observe expected failures caused by missing types.**
- [ ] **Step 3: Implement the smallest invariant-preserving aggregate and item types, including EF-compatible private constructors/backing collection without exposing mutable state.**
- [ ] **Step 4: Run the filtered tests and then the full test project.**

### Task 3: Application validation pipeline registration

**Files:**
- Create: `src/EcommerceApi.Application/DependencyInjection.cs`
- Create: `src/EcommerceApi.Application/Common/Behaviors/ValidationBehavior.cs`
- Create: `tests/EcommerceApi.Tests/Application/ValidationBehaviorTests.cs`

**Interfaces:**
- `ApplicationDependencyInjection.AddApplication(IServiceCollection)` registers MediatR and `IPipelineBehavior<,>` for `ValidationBehavior<,>` plus FluentValidation validators by assembly scanning.
- `ValidationBehavior<TRequest,TResponse>.Handle` executes all validators, throws `ValidationException` with aggregated failures, and does not call the next delegate when validation fails.

- [ ] **Step 1: Write a test request, validator, and delegate assertions proving validation runs and failed validation short-circuits.**
- [ ] **Step 2: Run the application test filter and observe failure before the behavior exists.**
- [ ] **Step 3: Implement the behavior and registration without adding future commands/handlers.**
- [ ] **Step 4: Run the filtered tests and full test project.**

### Task 4: EF Core SQLite persistence and migration

**Files:**
- Create: `src/EcommerceApi.Infrastructure/Persistence/OrderDbContext.cs`
- Create: `src/EcommerceApi.Infrastructure/Persistence/Configurations/OrderConfiguration.cs`
- Create: `src/EcommerceApi.Infrastructure/Persistence/Configurations/OrderItemConfiguration.cs`
- Create: `src/EcommerceApi.Infrastructure/Persistence/DesignTimeOrderDbContextFactory.cs`
- Create: `src/EcommerceApi.Infrastructure/DependencyInjection.cs`
- Create: `src/EcommerceApi.Infrastructure/Persistence/Migrations/*_InitialOrderSchema.cs`
- Create: `src/EcommerceApi.Infrastructure/Persistence/Migrations/OrderDbContextModelSnapshot.cs`
- Create: `tests/EcommerceApi.Tests/Infrastructure/OrderDbContextTests.cs`

**Interfaces:**
- `OrderDbContext` exposes `DbSet<Order>` and `DbSet<OrderItem>` only inside Infrastructure.
- `InfrastructureDependencyInjection.AddInfrastructure(IServiceCollection, IConfiguration)` registers SQLite with `ConnectionStrings:Orders`; relative paths are resolved against the configured content root by the API composition root.
- Database schema contains `Orders` and `OrderItems`, required FK/index/cascade relationship, status/quantity/price checks, and no `TotalAmount` column.

- [ ] **Step 1: Write real-SQLite tests for fresh migration, second idempotent migration, required relationship, and absence of `TotalAmount`.**
- [ ] **Step 2: Run the filtered tests and observe failure because the context/migration is absent.**
- [ ] **Step 3: Implement the context/configurations and generate `InitialOrderSchema` with EF tooling; do not use `EnsureCreated`.**
- [ ] **Step 4: Run the filtered tests against temporary SQLite database files and verify the migration history.**

### Task 5: API host, structural JWT, Problem Details, OpenAPI, and startup migration

**Files:**
- Create/modify: `src/EcommerceApi.Api/Program.cs`
- Create: `src/EcommerceApi.Api/Authentication/JwtOptions.cs`
- Create: `src/EcommerceApi.Api/Authentication/AuthenticationExtensions.cs`
- Create: `src/EcommerceApi.Api/ErrorHandling/GlobalExceptionHandler.cs`
- Create: `src/EcommerceApi.Api/Extensions/DatabaseMigrationExtensions.cs`
- Create: `src/EcommerceApi.Api/appsettings.json`, `src/EcommerceApi.Api/appsettings.Development.json`
- Create: `tests/EcommerceApi.Tests/Api/StartupConfigurationTests.cs`

**Interfaces:**
- `JwtOptions` requires configuration issuer, audience, lifetime, and a non-empty signing key of at least 32 bytes; the key is read only from configuration/environment and never committed.
- `DatabaseMigrationExtensions.ApplyDatabaseMigrationsAsync` creates a scope, calls `Database.MigrateAsync`, logs critical on failure, and rethrows.
- `Program` registers `AddProblemDetails`, global exception handler, `AddOpenApi`, `AddApplication`, `AddInfrastructure`, JWT bearer, authorization, and applies migrations before `Run`.
- No business route, login command, credential evaluator, token generator, order handler, or optional endpoint is added.

- [ ] **Step 1: Write tests for JWT configuration requirements and migration orchestration using a real SQLite context or focused test service provider.**
- [ ] **Step 2: Run the API test filter and observe the missing configuration/startup behavior.**
- [ ] **Step 3: Implement the host and middleware pipeline with no password/token logging and with startup abort on migration failure.**
- [ ] **Step 4: Run API tests and start locally with an environment signing key, observing migration before request serving.**

### Task 6: Docker, Compose, and README

**Files:**
- Create: `Dockerfile`
- Create: `docker-compose.yml`
- Modify: `README.md`

**Interfaces:**
- Compose starts only the API, sets `ConnectionStrings__Orders=Data Source=/app/data/ecommerce.db`, and mounts named volume `ecommerceapi-data:/app/data`.
- The image exposes port `8080`, runs as a non-root user, and creates a writable `/app/data` directory.
- README documents prerequisites, exact local/Docker commands, required non-production `JWT_SIGNING_KEY`, migration startup/idempotence, SQLite file locations, `docker compose down -v` reset, Minimal API rationale, Clean Architecture/CQRS registration, tests, excluded future features, and limitations.

- [ ] **Step 1: Add the multi-stage .NET 10 image and Compose volume/environment contract.**
- [ ] **Step 2: Document only behavior implemented in FEATURE-001 and explicitly defer login/orders.**
- [ ] **Step 3: Run `docker compose config` and inspect the rendered service, volume, environment, and port.**
- [ ] **Step 4: Run `docker compose build`, then start with a temporary non-production signing key and observe the API/container and SQLite file.**

### Task 7: Full verification and review handoff

**Files:**
- Modify only files required by verified defects from Tasks 1-6.

- [ ] **Step 1: Run `dotnet restore`.**
- [ ] **Step 2: Run `dotnet build --no-restore`.**
- [ ] **Step 3: Run `dotnet test --no-build`.**
- [ ] **Step 4: Inspect every project reference and search for prohibited technologies/usages (`EnsureCreated`, InMemory, PostgreSQL, Dapper, Npgsql, user seed).**
- [ ] **Step 5: Run `docker compose config` and `docker compose build`.**
- [ ] **Step 6: Capture fresh-database migration, restart/idempotence, Compose startup, and SQLite volume evidence.**
- [ ] **Step 7: Hand the same diff to independent reviewer and QA; correct only confirmed/accepted findings, then repeat relevant gates.**

