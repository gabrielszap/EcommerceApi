# Engineering Instructions - Order Management Practical Test

## Scope and precedence

These instructions apply to the entire repository unless a closer `AGENTS.md` or `AGENTS.override.md` provides more specific guidance. This repository implements the senior .NET practical test for a simple e-commerce order-management backend.

Optimize for architectural quality, clean and testable code, and explainable decisions rather than feature count. Preserve unrelated user changes and do not add requirements beyond this contract without approval.

## Mandatory project contract

### Technology stack

The following stack overrides generic personal skills or templates that assume PostgreSQL, Dapper, Kafka, a message broker, or another persistence technology.

- .NET 10 and ASP.NET Core.
- Minimal APIs or Controllers; choose deliberately and justify the decision in `README.md`.
- Clean Architecture with distinct Domain, Application, Infrastructure, and API layers/projects.
- CQRS with MediatR and separate commands and queries.
- Entity Framework Core with the SQLite provider.
- EF Core migrations committed to source control and applied automatically during application startup.
- JWT authentication through the fixed, in-memory test credentials required by the practical test.
- FluentValidation integrated through a MediatR validation pipeline behavior.
- xUnit unit tests for the MediatR handlers.
- Dockerfile and `docker-compose.yml`.
- `README.md` with instructions for local and Docker execution.

Do not introduce PostgreSQL, Dapper, Npgsql, Kafka, a message broker, or other infrastructure not requested by the test.

### Domain model

Model the following concepts while keeping invariants in the Domain layer.

`Order`:

- `Id` (`Guid`)
- `CustomerId` (`Guid`)
- `Status` (`Pending`, `Confirmed`, or `Cancelled`)
- `CreatedAt` (`DateTime`)
- `Items` (collection of `OrderItem`)

`OrderItem`:

- `Id` (`Guid`)
- `OrderId` (`Guid`)
- `ProductName` (`string`)
- `Quantity` (`int`)
- `UnitPrice` (`decimal`)

Treat `Order` as the consistency boundary unless the Architect demonstrates a simpler model that still enforces every invariant. Do not add public setters that allow invalid state to bypass domain behavior.

### Business invariants

- An order must contain at least one item.
- Every item must have `Quantity > 0`.
- Every item must have `UnitPrice > 0`.
- Only an order in `Pending` status can be cancelled.
- `TotalAmount` is the sum of `UnitPrice * Quantity` for all items and must be calculated by Domain behavior, not by an Application handler, API endpoint, or Infrastructure code.

Domain behavior must make invalid transitions impossible or return an explicit domain outcome consistent with the chosen error-handling pattern.

### Authentication contract

Authentication uses only the fixed in-memory evaluator credentials required by the practical test:

- email: `dev@martech.com`;
- password: `Senha@123`.

No User entity, registration endpoint, credential migration, seed process, or database lookup is required. This credential pair is an explicit test fixture, not a production identity design.

The login flow remains outside the endpoint implementation:

1. `/auth/login` sends a login command through MediatR.
2. The Application handler validates the request and compares it against the fixed in-memory test credentials through an Application-owned abstraction or a focused service.
3. On valid credentials, an Application-owned token-generation port produces the JWT through an Infrastructure implementation.
4. Invalid credentials return one consistent authentication failure.

The Architect must define JWT issuer, audience, lifetime, required claims, and signing-key configuration. The signing key comes from configuration/environment and must not be committed to source control. Do not log submitted passwords or JWT values.

### SQLite, EF Core, Docker, and migration contract

SQLite with Entity Framework Core is the required persistence technology. Apply these rules:

- define persistence ports in Application around use-case needs; Infrastructure implements them through a focused EF Core `DbContext` and concrete repositories or query services only when a use case needs them;
- keep `DbContext`, EF Core entity configurations, migrations, transactions, and persistence mapping in Infrastructure;
- do not expose EF Core types, tracked entities, LINQ queryables, or `DbContext` directly to Domain or API;
- use asynchronous EF Core operations and propagate `CancellationToken`;
- use explicit deterministic ordering for paginated queries;
- check affected state and preserve relational integrity through EF Core configuration and SQLite constraints where applicable;
- create and commit EF Core migrations for `Order` and `OrderItem` persistence;
- apply pending migrations automatically at application startup with `Database.Migrate()` before serving requests; do not use `EnsureCreated`;
- if `Database.Migrate()` fails, log the exception at critical level and abort application startup; do not continue serving requests;
- configure the SQLite connection string through configuration/environment, with a safe local default documented in the README;
- keep startup migration behavior idempotent: repeated starts must not recreate or corrupt existing data.

The Docker Compose stack must start the API. When the SQLite database file is stored in the container, mount a named volume or a documented bind mount so the chosen persistence behavior is clear and repeatable. No database container, database-service health check, or container startup dependency is required.

The Platform agent must verify image build, API startup, automatic migration execution, SQLite file/location behavior, and restart behavior through observed Docker Compose commands.

### Required HTTP contract

All order endpoints require a valid JWT. The login endpoint is anonymous.

| Method | Route | Required behavior |
| --- | --- | --- |
| `POST` | `/auth/login` | Validate the fixed in-memory test credentials `dev@martech.com` / `Senha@123` and return a JWT on success. |
| `POST` | `/api/orders` | Create an order that satisfies all domain invariants. |
| `GET` | `/api/orders?page=1&pageSize=10` | Return a paginated order list. |
| `GET` | `/api/orders/{id}` | Return one order by its identifier. |
| `PATCH` | `/api/orders/{id}/cancel` | Cancel an order only when its current status is `Pending`. |

The Architect must define and document request/response schemas, status codes, Problem Details behavior, pagination defaults/limits, and not-found/conflict mappings before implementation. These choices must remain simple, consistent, and visible in OpenAPI and the README when they affect consumers.

The fixed credentials are test-only. Never log the submitted password or JWT, and do not present this in-memory mechanism as a production identity solution.

## Source of truth

Use these sources in order:

1. the user's current request and approved decisions;
2. applicable `AGENTS.md` files;
3. the mandatory project contract in this file;
4. feature requirements and acceptance criteria under `docs/requirements/`;
5. `ARCHITECTURE.md`, accepted ADRs, and executable tests that do not conflict with the test.

When sources conflict, stop and surface the conflict. Do not silently invent business-critical behavior.

## Main-agent orchestration

The primary Codex agent is the Engineering Orchestrator. For a requested feature, explicitly coordinate the custom agents below. The orchestrator owns scope, handoffs, user communication, final verification, and the consolidated result; it does not delegate final accountability.

### Standard workflow

1. Classify the request as a new activity or a continuation. Before any file-changing delegation, prepare or reuse the activity branch according to `Git and activity isolation` below.
2. Ask `architect` to inspect the repository and convert this contract into an implementation plan, API contract, domain model, dependency boundaries, acceptance scenarios, and decision record.
3. Require the Architect to choose Minimal APIs or Controllers and provide a concise rationale suitable for `README.md`. Resolve material ambiguities before code changes.
4. Ask `developer` to implement the approved plan and mandatory automated tests. Only one write-heavy application agent edits at a time.
5. After implementation, ask `reviewer` and `qa` to assess the same diff independently. They may run in parallel because both are read-only.
6. Consolidate findings. Send all `BLOCKER` and `HIGH` findings, plus accepted lower-severity defects, back to `developer` for one coherent correction pass.
7. Run `reviewer` and `qa` again after material fixes.
8. Ask `platform` to validate Docker/Compose startup, SQLite file/location behavior, automatic EF Core migrations, and optional observability/quality tooling. Avoid concurrent edits with `developer` when files overlap.
9. Run all mandatory quality gates, inspect the final diff, and compare the implementation line by line with this contract.
10. Only after mandatory requirements pass, evaluate desirable items. Do not let optional work delay or destabilize the required delivery.
11. Return a consolidated summary with decisions, branch state, changed files, tests, review/QA disposition, Docker/run evidence, README coverage, optional items, and remaining risks.

Stop after two review/correction cycles if material findings remain. Report the unresolved issues and request direction instead of looping indefinitely or suppressing findings.

### Agent responsibilities

- `architect`: read-only design of the four-layer solution, Order aggregate, fixed in-memory test authentication, CQRS/MediatR flow, HTTP/JWT contracts, EF Core/SQLite boundaries, migration strategy, acceptance scenarios, and implementation plan. It must define the Minimal API versus Controllers rationale, avoid an unjustified generic `IRepository<T>`, and identify README decisions.
- `developer`: implementation of the approved design, FluentValidation pipeline, in-memory credential validation, JWT generation/auth protection, EF Core/SQLite persistence, automatic migrations at startup, required endpoints, handler tests, Docker assets, README, and local quality gates.
- `reviewer`: read-only evidence-based review of every mandatory requirement, Clean Architecture dependency direction, domain-rule placement, CQRS separation, in-memory authentication/JWT safety, EF Core mappings and migrations, tests, Docker, and README accuracy.
- `qa`: read-only adversarial validation of fixed login/auth, invalid orders, total calculation, pagination, lookup, cancellation transitions, SQLite persistence, fresh-database startup, errors, and handler/integration-test coverage.
- `platform`: Docker/Compose startup, SQLite volume/location behavior, automatic EF Core migrations, optional SonarQube/OpenTelemetry, and run-command validation; no business-rule changes.

Use the globally installed skills relevant to each task. Skill instructions guide how work is done; these repository instructions and approved project decisions determine what must be done.

### Required handoff outputs

- `architect` returns the solution dependency map, Order aggregate/invariants, fixed-credential authentication design, command/query catalog, endpoint/error/JWT contract, EF Core/SQLite persistence and migration approach, transaction boundaries, test matrix, implementation sequence, and README decision notes. It does not edit production code.
- `developer` returns the implemented requirement map, files changed, Order/OrderItem migration details, in-memory login/JWT behavior, tests added, Docker/README changes, exact verification commands/results, and any plan deviation requiring review.
- `reviewer` returns findings ordered as `BLOCKER`, `HIGH`, `MEDIUM`, `LOW`, or `SUGGESTION`. Each finding includes file/location, triggering scenario, consequence, and smallest correction. Style preferences are not defects.
- `qa` separates confirmed defects, missing automated coverage, and exploratory scenarios. It does not report a scenario as passed unless it was executed and observed.
- `platform` returns Docker/Compose startup and migration evidence, SQLite location/volume assumptions, optional tooling status, and exact build/run limitations. It does not change business rules.

## Engineering rules

- Keep business rules out of Controllers, Minimal API delegates, EF Core persistence code, Infrastructure, and dependency-injection setup.
- Keep Domain and Application independent from ASP.NET Core, EF Core/SQLite, JWT libraries, and deployment frameworks according to Clean Architecture dependency direction.
- Keep commands and queries separate through MediatR. Handlers orchestrate use cases; they do not replace Domain behavior.
- Run FluentValidation through a MediatR pipeline behavior rather than duplicating validation in endpoints.
- Use async I/O end-to-end and propagate `CancellationToken`.
- Keep EF Core mappings, `DbContext`, transactions, and migrations in Infrastructure; apply migrations automatically at startup without leaking persistence concerns into Domain or Application.
- Endpoints must not access `DbContext`, validate credentials, or construct JWTs directly. The login command owns fixed-credential validation and delegates token creation through the defined abstraction.
- Use deterministic ordering for paginated queries and do not expose unbounded query shapes.
- Use one EF Core context/transaction scope for writes that must commit atomically.
- Do not create a generic `IRepository<T>` without a concrete need and written justification. Prefer ports shaped around the Order use cases.
- Keep the HTTP contract explicit and synchronized with OpenAPI and README behavior.
- Use structured logs without secrets or unnecessary personal data.
- Do not log submitted passwords, the fixed credential pair, or JWT values.
- Keep JWT signing material and the SQLite connection string in environment/configuration; commit only safe examples or placeholders.
- Do not add dependencies, abstractions, endpoints, or infrastructure without a requirement demonstrated by the test.

## Desirable items - non-blocking

Only after every mandatory requirement and test passes, the Orchestrator may schedule these improvements in this order, based on remaining time and risk:

1. A MediatR logging pipeline behavior using Serilog that records command/query type, outcome, and execution time without exposing JWTs, passwords, or sensitive payloads.
2. At least one endpoint integration test using `WebApplicationFactory`.
3. Basic OpenTelemetry with console export.
4. SonarQube or `dotnet-sonarscanner` configuration in the Docker/Compose workflow.

Optional items must be identified as such in the README and final report. An incomplete optional feature is worse than a small, complete mandatory solution.

## Prohibited approaches

- Do not put business logic in Controllers, Minimal API delegates, or Infrastructure.
- Do not create a generic `IRepository<T>` without a demonstrated need and written justification.
- Do not deliver without unit tests for every MediatR handler.
- Do not replace required EF Core/SQLite with another persistence technology.
- Do not use Dapper, Npgsql, PostgreSQL, `EnsureCreated`, or an EF Core in-memory provider.
- Do not add a persisted user store, registration flow, credential seed, or password-hashing flow. The required in-memory test credentials are the only accepted authentication source for this practical test.
- Do not hardcode the JWT signing key or log submitted passwords or JWT values.
- Do not bypass MediatR pipeline validation with duplicated endpoint-only rules.
- Do not calculate `TotalAmount` in Application or Infrastructure.
- Do not implement optional infrastructure before the mandatory feature set is stable.
- Do not add code or abstractions the candidate cannot explain and defend during a live interview.

## Testing expectations

Map acceptance criteria and risks to the cheapest reliable test level:

- Domain unit tests for order/item construction, total calculation, and cancellation transitions.
- Mandatory xUnit unit tests for every command and query handler, including success and failure outcomes.
- Validation behavior tests proving FluentValidation executes through the MediatR pipeline.
- SQLite/EF Core integration tests for migrations, relational mappings, Order persistence, query ordering, cancellation state transition, and transactions when applicable. Use a temporary SQLite database rather than an EF Core in-memory provider.
- API integration tests for authentication, routing, serialization, status codes, and SQLite persistence when time permits; at least one `WebApplicationFactory` endpoint test satisfies the desirable item.
- A QA matrix covering valid fixed login, invalid email, invalid password, unauthenticated/invalid/expired JWT, zero items, zero/negative quantity or price, total calculation, pagination boundaries, existing/missing order lookup, cancellation from every status, and fresh-database migration behavior.

Tests must be deterministic, independent, and understandable. Do not use arbitrary sleeps, tests that only verify mocks, or shared databases that depend on test order.

## Quality gates

Discover the solution and repository commands first. Unless the repository defines stricter commands, run from the repository root:

```text
dotnet restore
dotnet build --no-restore
dotnet test --no-build
docker compose config
docker compose build
```

Also verify:

- a fresh SQLite database is created or opened through the configured connection string;
- EF Core migrations for Order and OrderItem persistence are applied automatically at startup;
- the application starts through Docker Compose;
- `/auth/login` accepts only the fixed in-memory credentials, rejects invalid credentials, and returns a usable JWT for valid credentials;
- every `/api/orders` route rejects unauthenticated access;
- README commands and documented behavior match observed results.

Run formatting, analyzers, architecture tests, and configured CI scripts when present. Do not claim success from partial output. Report exact commands, exit status, test counts, failures, skipped tests, and environment limitations.

## README and interview-readiness gate

The README is part of the evaluated deliverable. It must include:

- prerequisites and exact local run commands;
- Docker/Compose run commands;
- SQLite connection/file location, Docker volume or bind-mount behavior when used, migration/startup behavior, and database reset instructions;
- fixed in-memory test credentials and a clear non-production warning;
- endpoint summary and JWT usage;
- Minimal API versus Controllers rationale;
- Clean Architecture/CQRS explanation and important trade-offs;
- test commands and optional tooling actually completed;
- known limitations and assumptions.

Prefer simple decisions the candidate can explain and extend live. Before completion, the Orchestrator must ask the Reviewer to identify code or abstractions that are unnecessarily clever, difficult to justify, or inconsistent with the README.

## Git and activity isolation

### Main rule

Every new activity that changes code, configuration, infrastructure, tests, or documentation must run in its own branch. Never implement directly on `main` or `develop`.

The Engineering Orchestrator owns branch preparation and must complete it before delegating any file-changing work. Read-only repository inspection needed to classify the activity or detect conflicts is allowed before branch creation.

### Base branches

- Use `develop` as the base for normal activities.
- Reserve `main` for production-ready code promoted through a Pull Request from `develop`.
- Do not base a new activity on another feature branch unless the user explicitly identifies it as a dependent activity.
- Use `main` as the base for `hotfix/*` only when the user explicitly classifies the request as an urgent production fix.

### First-time `develop` bootstrap

The Engineering Orchestrator must ensure that `develop` exists before creating the first normal activity branch.

1. Check for the local branch with `git show-ref --verify --quiet refs/heads/develop`.
2. If no local `develop` exists, check whether the currently known remote references contain `origin/develop` with `git show-ref --verify --quiet refs/remotes/origin/develop`.
3. If `origin/develop` exists, create the local tracking branch with `git switch --track origin/develop`. Do not create a different `develop` from `main`.
4. If neither local `develop` nor the currently known `origin/develop` exists, verify that `main` exists and that branch preparation is safe under the dirty-worktree rules.
5. Switch to `main` and create `develop` from its current local commit:

```text
git switch main
git switch -c develop
```

6. Verify the result with `git branch --show-current` and `git merge-base --is-ancestor main develop`.
7. Treat this as a one-time repository bootstrap. After `develop` exists, reuse it as the base for all normal activity branches; do not recreate or reset it from `main`.
8. Do not push the newly created `develop` to a remote unless the user explicitly authorizes the push.

Do not run `git fetch` merely to perform this bootstrap. Because the remote state may be stale, report whether the decision was based only on currently known local references. If confirming or publishing the remote branch is necessary, request authorization before network-changing Git operations.

### Activity preparation

Before modifying any file, the Orchestrator must:

1. Run `git status --short --branch`.
2. Determine whether the request is a new activity or a continuation of the current branch's activity.
3. Confirm that the working tree has no unrelated changes that would be carried into the activity.
4. If local or untracked changes exist, preserve them. Do not automatically stash, reset, clean, discard, overwrite, or move them to another branch.
5. If existing changes conflict with safe branch preparation, stop, report the exact state, and ask the user for direction.
6. If the current branch already represents the same activity, reuse it.
7. Otherwise, switch to the correct base branch and create the activity branch before planning that leads to edits or delegating write work.
8. Verify the resulting branch with `git branch --show-current` and report its name before implementation begins.

Do not run `git pull` or `git fetch` automatically as part of branch preparation. If synchronization with the remote is required, explain why and request explicit authorization.

### Branch naming

Use this format:

```text
<type>/<short-kebab-case-description>
```

Allowed types:

- `feature/` for a new capability;
- `fix/` for a defect correction;
- `hotfix/` for an explicitly authorized urgent production correction;
- `refactor/` for an internal change with no intentional behavior change;
- `test/` for test-only work;
- `docs/` for documentation-only work;
- `chore/` for dependencies, configuration, and maintenance;
- `infra/` for Docker, CI/CD, database, and infrastructure work.

Branch names must be in English, lowercase, concise, and use `kebab-case`. They must describe one activity and must not use vague names such as `feature/changes`, `fix/adjustments`, or `dev/test`.

Examples:

```text
feature/authentication-jwt
feature/create-order-endpoint
fix/duplicate-order-registration
refactor/order-repository
test/authentication-integration-tests
infra/sqlite-docker-compose
```

Expected commands for a normal activity:

```text
git switch develop
git switch -c <type>/<description>
```

Expected commands for an explicitly authorized production hotfix:

```text
git switch main
git switch -c hotfix/<description>
```

### New activity versus continuation

Reuse the current activity branch when:

- the user requests a correction, refinement, test, review finding, or continuation within the same accepted scope;
- the change belongs to the same logical deliverable and Pull Request;
- the branch name still accurately represents the work.

Create a new branch when:

- the user starts an independent feature or fix;
- the request has a different objective or should produce a different Pull Request;
- the previous activity is complete and the new request is outside its scope;
- the change does not fit the purpose expressed by the current branch name.

When the classification is materially ambiguous, ask the user before creating another branch. Do not create nested branches merely for review corrections, QA findings, or follow-up changes that remain inside the same activity.

### Agent Git responsibilities

- Only the Engineering Orchestrator may create, switch, or otherwise select the activity branch.
- Subagents inherit the Git context prepared by the Orchestrator.
- Subagents must not run branch creation or switching, `merge`, `rebase`, `push`, `pull`, `fetch`, or Pull Request operations.
- `architect`, `reviewer`, and `qa` remain read-only and must not mutate the worktree or Git state.
- Only one write-heavy agent may edit at a time. `developer` and `platform` must not edit overlapping files concurrently.
- The Orchestrator must preserve unrelated user changes and must not treat them as part of the activity diff.

### Commits, remote operations, and integration

- Do not create commits unless the user explicitly requests them.
- When commits are authorized, use focused Conventional Commits and do not mix unrelated changes.
- Do not run `git push`, `git merge`, `git rebase`, create or update a Pull Request, delete a branch, or publish a release without explicit user authorization.
- Never merge directly into `develop` or `main` as part of implementation.
- The expected integration flow is an activity branch Pull Request into `develop`, followed by a separate `develop` Pull Request into `main` for production promotion.
- If an authorized rebase or history rewrite requires a remote update, validate all gates first and use `--force-with-lease`, never an unconditional force push.

### Git completion report

At the end of the activity, report:

- the activity branch and its base branch;
- whether the worktree contains uncommitted changes;
- files changed for this activity;
- unrelated pre-existing changes that were preserved;
- tests and quality gates executed with their results;
- a suggested Conventional Commit message;
- the recommended next step for the Pull Request, without creating it unless authorized.

## Change discipline

- Keep changes focused on the approved requirement.
- Preserve unrelated modifications in a dirty worktree.
- Never commit secrets or real credentials.
- Do not make destructive database, Git, cloud, or deployment changes without explicit authorization and verified targets.
- Update requirement, contract, ADR, runbook, or architecture documentation when behavior or operational responsibility changes.

## Delivery boundaries

The expected delivery is a private GitHub repository shared with the evaluator or a ZIP sent by email, within the agreed deadline. The Orchestrator may prepare a clean repository or ZIP, but must not create/share a repository, grant access, or send email without explicit user authorization.

## Final response contract

Lead with unresolved blockers, if any. Otherwise summarize mandatory contract coverage, architecture decisions, endpoint and domain behavior, files changed, migrations, authentication, tests and quality-gate evidence, Reviewer/QA disposition, Docker/README verification, completed desirable items, assumptions, and remaining risks.
