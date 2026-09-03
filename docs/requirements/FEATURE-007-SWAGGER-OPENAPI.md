# FEATURE-007 - Swagger/OpenAPI Documentation

## Activity and Git context

- Activity classification: new activity.
- Branch: `docs/swagger-openapi-spec`.
- Base branch: `develop`.
- Expected Pull Request target: `develop`.
- Depends on: `FEATURE-002-AUTHENTICATION-JWT.md`, `FEATURE-003-CREATE-ORDER.md`, `FEATURE-004-QUERY-ORDERS.md`, and `FEATURE-005-CANCEL-ORDER.md`.

This requirement file does not authorize commits, push, merge, rebase, branch deletion, Pull Request creation, or release publication. Those operations require an explicit user request.

## Goal

Define a concise and accurate Swagger/OpenAPI configuration for the existing order-management API so an evaluator or API consumer can discover the contract, authenticate with JWT Bearer in Swagger UI, execute the required endpoints, and understand success and error responses without changing business behavior.

## Context

The API already uses ASP.NET Core Minimal APIs and `Microsoft.AspNetCore.OpenApi` to expose `/openapi/v1.json`. Endpoint groups already declare tags, summaries, descriptions, status codes, pagination metadata, GUID parameter metadata, and a Bearer security scheme. The missing activity is to formalize and complete the expected OpenAPI behavior, add an interactive Swagger UI plan, tighten metadata and examples, and define when documentation endpoints are exposed across environments.

Swagger/OpenAPI remains an API-host concern. Domain, Application, Infrastructure, MediatR handlers, EF Core mappings, SQLite migrations, JWT generation, and order business rules must remain unchanged.

## Scope

### Included

- Keep first-party .NET OpenAPI document generation through `Microsoft.AspNetCore.OpenApi`.
- Add Swagger UI as a lightweight interactive viewer for the generated `/openapi/v1.json` document.
- Configure API metadata for title, version, description, tags, and security scheme.
- Document every required endpoint with summaries, descriptions, request and response schemas, response status codes, and relevant examples.
- Make JWT Bearer authentication usable from Swagger UI through the `Authorize` button.
- Document validation, authentication, not-found, conflict, and unexpected-error responses with Problem Details.
- Define environment behavior for Development, Production, and automated tests.
- Add API integration tests that assert key OpenAPI contract elements.
- Update README after implementation to document Swagger UI, OpenAPI URL, JWT usage, and production exposure rules.

### Excluded

- Domain, Application, Infrastructure, MediatR handler, EF Core, SQLite migration, Docker, or business-rule changes.
- A new API versioning strategy.
- Generated client SDKs.
- Persisting a generated OpenAPI JSON/YAML artifact in source control.
- Replacing the current OpenAPI generator with NSwag or a full Swashbuckle generator setup.
- Adding Scalar, ReDoc, OAuth, refresh tokens, registration, persisted users, or production identity behavior.

## Architecture and affected areas

Swagger/OpenAPI configuration belongs only in `EcommerceApi.Api`.

Expected architectural decisions:

1. Keep `Microsoft.AspNetCore.OpenApi` as the OpenAPI document generator.
   - Rationale: the project targets .NET 10, already uses Minimal APIs and the built-in OpenAPI package, and the API surface is small.
   - Trade-off: richer third-party filter APIs are avoided; examples and metadata should be implemented with endpoint, operation, schema, or document transformers where needed.

2. Add only the UI package needed to serve Swagger UI.
   - Recommended package: `Swashbuckle.AspNetCore.SwaggerUI`.
   - Rationale: ASP.NET Core can generate the OpenAPI document but does not provide Swagger UI by itself.
   - Trade-off: one third-party dependency is accepted for the browser UI while the document generation remains owned by ASP.NET Core OpenAPI.

3. Keep endpoint delegates thin.
   - OpenAPI metadata may live beside Minimal API route mapping or in API-layer extension methods.
   - No OpenAPI attributes, documentation-only DTOs, or framework dependencies may be introduced into Domain, Application, or Infrastructure.

4. Do not create an ADR for this activity unless implementation uncovers a broader architecture decision.
   - The choice is scoped to API documentation and should be captured in this feature spec and README.

Expected future files affected:

- `Directory.Packages.props`: add the Swagger UI package version if needed.
- `src/EcommerceApi.Api/EcommerceApi.Api.csproj`: reference the Swagger UI package.
- `src/EcommerceApi.Api/Program.cs`: configure document metadata, environment-gated `MapOpenApi()`, and Swagger UI.
- `src/EcommerceApi.Api/OpenApi/*`: optional API-layer extension methods for reusable OpenAPI configuration.
- `src/EcommerceApi.Api/Authentication/AuthenticationEndpoints.cs`: enrich login operation metadata and examples if not centralized.
- `src/EcommerceApi.Api/Orders/OrderEndpoints.cs`: enrich order operation metadata, examples, response content types, pagination bounds, and GUID schemas if not centralized.
- `src/EcommerceApi.Api/Properties/launchSettings.json`: optionally launch `/swagger` in Development.
- `tests/EcommerceApi.Tests/Api/OpenApiDocumentTests.cs`: verify generated OpenAPI contract.
- `README.md`: document Swagger/OpenAPI usage and environment behavior.

Files that should remain unchanged unless a later implementation approval explicitly expands scope:

- `src/EcommerceApi.Domain/**`
- `src/EcommerceApi.Application/**`
- `src/EcommerceApi.Infrastructure/**`
- `src/EcommerceApi.Infrastructure/Persistence/Migrations/**`
- `Dockerfile`
- `docker-compose.yml`

## API contract

### OpenAPI document

`GET /openapi/v1.json` returns the generated OpenAPI document when OpenAPI is enabled for the current environment.

Document metadata:

- Title: `EcommerceApi`
- Version: `v1`
- Description: order-management practical-test API using ASP.NET Core Minimal APIs, Clean Architecture, CQRS with MediatR, JWT Bearer authentication, EF Core, and SQLite.
- Tags:
  - `Authentication`
  - `Orders`
- Servers:
  - Do not hardcode production URLs.
  - Let local and deployed hosts be inferred from the request unless an explicit server URL is approved later.

The document must not expose implementation types from EF Core, SQLite, Domain entities, internal exceptions, secrets, signing keys, or JWT values.

### Swagger UI

`GET /swagger` serves Swagger UI when Swagger UI is enabled for the current environment.

Swagger UI must:

- load `/openapi/v1.json`;
- show the API title and version;
- expose the `Authorize` button for JWT Bearer authentication;
- allow a user to call `POST /auth/login`, paste the returned JWT, and execute protected order endpoints;
- avoid pre-populating or persisting real JWTs;
- avoid displaying submitted passwords or tokens in custom UI text beyond request examples required by the evaluator.

## Endpoint documentation

### `POST /auth/login`

Anonymous.

Request example:

```json
{
  "email": "dev@martech.com",
  "password": "Senha@123"
}
```

Successful response: `200 OK`

```json
{
  "accessToken": "<jwt>",
  "expiresAtUtc": "2026-09-02T13:00:00Z"
}
```

Documented responses:

- `200 OK`: JWT issued for the fixed evaluator credentials.
- `400 Bad Request`: validation Problem Details for invalid request shape.
- `401 Unauthorized`: invalid credentials Problem Details.

The documentation must clearly state that the fixed credentials are an evaluator test fixture and not a production identity design.

### `POST /api/orders`

Requires `Authorization: Bearer <accessToken>`.

Request example:

```json
{
  "customerId": "11111111-1111-1111-1111-111111111111",
  "items": [
    {
      "productName": "Keyboard",
      "quantity": 2,
      "unitPrice": 150.00
    },
    {
      "productName": "Mouse",
      "quantity": 1,
      "unitPrice": 75.50
    }
  ]
}
```

Successful response: `201 Created`

- Header: `Location: /api/orders/{id}`
- Body includes `id`, `customerId`, `status`, `createdAt`, `items`, and Domain-calculated `totalAmount`.

Documented responses:

- `201 Created`
- `400 Bad Request`: validation or domain invariant Problem Details.
- `401 Unauthorized`

### `GET /api/orders?page=1&pageSize=10`

Requires `Authorization: Bearer <accessToken>`.

Query parameters:

- `page`: integer, default `1`, minimum `1`.
- `pageSize`: integer, default `10`, minimum `1`, maximum `100`.

Successful response: `200 OK`

```json
{
  "items": [
    {
      "id": "22222222-2222-2222-2222-222222222222",
      "customerId": "11111111-1111-1111-1111-111111111111",
      "status": "Pending",
      "createdAt": "2026-09-02T12:00:00Z",
      "itemCount": 2,
      "totalAmount": 375.50
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

Documented responses:

- `200 OK`
- `400 Bad Request`: invalid pagination Problem Details.
- `401 Unauthorized`

### `GET /api/orders/{id}`

Requires `Authorization: Bearer <accessToken>`.

Path parameter:

- `id`: string with `uuid` format; empty GUID is invalid.

Documented responses:

- `200 OK`: full order details.
- `400 Bad Request`: malformed or empty GUID Problem Details.
- `401 Unauthorized`
- `404 Not Found`: unknown valid GUID Problem Details.

### `PATCH /api/orders/{id}/cancel`

Requires `Authorization: Bearer <accessToken>` and has no request body.

Path parameter:

- `id`: string with `uuid` format; empty GUID is invalid.

Documented responses:

- `200 OK`: cancelled order representation.
- `400 Bad Request`: malformed or empty GUID Problem Details.
- `401 Unauthorized`
- `404 Not Found`: unknown valid GUID Problem Details.
- `409 Conflict`: the order is already `Cancelled` or `Confirmed`.

## Security and privacy

OpenAPI must define a reusable security scheme:

- Name: `Bearer`
- Type: `http`
- Scheme: `bearer`
- Bearer format: `JWT`
- Description: `Use the token returned by POST /auth/login as: Authorization: Bearer <accessToken>.`

Rules:

- `POST /auth/login` must not require authentication in the OpenAPI document.
- Every `/api/orders` operation must declare the Bearer security requirement.
- The Swagger UI authorization value must be entered by the user; it must not be prefilled.
- Swagger/OpenAPI code must not log submitted passwords, access tokens, signing keys, or full request bodies containing credentials.
- The OpenAPI document may include the fixed evaluator credentials only as test-only documentation for the login example.

## Failure behavior

All documented application error responses must use Problem Details and `application/problem+json`.

Common Problem Details example:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "Safe client-facing detail when applicable.",
  "instance": "/api/orders"
}
```

Validation Problem Details also includes field errors:

```json
{
  "errors": {
    "Items[0].Quantity": [
      "'Quantity' must be greater than '0'."
    ]
  }
}
```

Unexpected `500 Internal Server Error` responses should be documented generically when represented in the OpenAPI contract. They may include a `traceId` if the current error handler emits one, but they must not expose stack traces, SQL, secrets, JWTs, signing keys, internal exception type names, or implementation details.

## Environment behavior

Development:

- `/openapi/v1.json` is exposed.
- `/swagger` is exposed.
- Swagger UI points to `/openapi/v1.json`.
- `launchSettings.json` may launch `/swagger`.

Production:

- Swagger UI is not exposed by default.
- `/openapi/v1.json` is not exposed by default unless explicitly enabled with a documented configuration flag such as `OpenApi:Enabled=true`.
- Enabling documentation endpoints in Production is an operational decision and must be documented.

Automated tests:

- OpenAPI contract tests may run under Development or with explicit `OpenApi:Enabled=true`.
- Tests must not require a real JWT signing secret beyond the existing safe test configuration approach.

Compatibility:

- Business endpoints and DTOs remain unchanged.
- Disabling documentation endpoints by default in Production changes only documentation endpoint exposure. README must document how to enable them intentionally if needed.

## Persistence and consistency

No persistence changes are required.

- No EF Core migration is needed.
- SQLite schema and data remain unchanged.
- Startup database migration behavior remains unchanged.
- OpenAPI examples must not imply persisted seed data.

## Observability

No new observability infrastructure is required.

Swagger/OpenAPI configuration should rely on existing ASP.NET Core and Serilog behavior. It must not add request-body logging or log secrets. Any future logs around documentation endpoint enablement should be structured and must not include credentials or token values.

## Compatibility and rollout

The change is backward compatible for API consumers because it does not alter the existing business endpoints. The only intended runtime behavior change is documentation endpoint exposure:

- Development gets an interactive `/swagger` UI.
- Production hides OpenAPI and Swagger UI by default unless explicitly enabled.

Rollback is simple: remove or disable the API-layer Swagger UI package/configuration and restore the previous `MapOpenApi()` behavior. No data rollback is required.

## Acceptance criteria

- Given the API runs in Development, when `GET /openapi/v1.json` is requested, then it returns `200 OK` with a valid OpenAPI JSON document.
- Given the API runs in Development, when `GET /swagger` is requested, then Swagger UI loads and uses `/openapi/v1.json`.
- Given the OpenAPI document, when its metadata is inspected, then it contains title `EcommerceApi`, version `v1`, tags for `Authentication` and `Orders`, endpoint names, summaries, descriptions, and documented response codes.
- Given the OpenAPI document, when security schemes are inspected, then it defines a `Bearer` HTTP JWT scheme with `scheme: bearer` and `bearerFormat: JWT`.
- Given the OpenAPI document, when operation security is inspected, then every `/api/orders` operation requires Bearer security and `POST /auth/login` does not.
- Given Swagger UI in Development, when a user calls `POST /auth/login` with the fixed test credentials and authorizes with the returned token, then the protected order endpoints can be executed from the UI.
- Given documented examples, when compared with DTOs and README examples, then JSON casing and fields match the actual API contract.
- Given documented error responses, when compared with endpoint behavior, then validation, authentication, not-found, conflict, and generic server-error responses use Problem Details-compatible shapes.
- Given Production default configuration, when `/swagger` and `/openapi/v1.json` are requested, then they are not exposed unless explicitly enabled.
- Given automated tests, when they inspect the generated OpenAPI document, then they cover metadata, Bearer security, protected operations, login anonymity, pagination bounds, GUID format, and key response status codes.

## Test and verification matrix

- Domain tests: none required; no domain behavior changes.
- Command/query handler tests: none required; no handler behavior changes.
- Validation pipeline tests: none required unless implementation changes validation metadata generation.
- SQLite/EF Core integration tests: none required; no persistence changes.
- API integration tests:
  - `GET /openapi/v1.json` in Development returns `200 OK` and JSON.
  - document `info.title` is `EcommerceApi` and `info.version` is `v1`.
  - `components.securitySchemes.Bearer` has `type=http`, `scheme=bearer`, and `bearerFormat=JWT`.
  - `POST /auth/login` documents `200`, `400`, and `401` and has no security requirement.
  - `POST /api/orders`, `GET /api/orders`, `GET /api/orders/{id}`, and `PATCH /api/orders/{id}/cancel` require Bearer security.
  - pagination parameters document integer type, defaults, minimums, and `pageSize` maximum.
  - `{id}` parameters document string `uuid` format.
  - documented error responses include `application/problem+json`.
  - Production default does not expose Swagger/OpenAPI endpoints when the environment gate is implemented.
- Docker/Compose verification:
  - no mandatory Docker change is expected;
  - if a production OpenAPI opt-in variable is added to Compose, verify and document it explicitly.
- Security and authorization checks:
  - Swagger UI does not prefill JWT values;
  - login remains anonymous;
  - order routes remain protected both at runtime and in the OpenAPI document.
- Required quality gates after implementation:
  - `dotnet restore`
  - `dotnet build --no-restore`
  - `dotnet test --no-build`
  - `docker compose config`
  - `docker compose build`

## Documentation and operational impact

README must be updated after implementation to include:

- Development Swagger UI URL: `/swagger`.
- OpenAPI document URL: `/openapi/v1.json` when enabled.
- How to use `POST /auth/login` and the Swagger UI `Authorize` button with `Bearer <accessToken>`.
- Production default behavior and the explicit configuration needed to expose documentation endpoints outside Development, if supported.
- A note that fixed credentials are test-only and JWTs/signing keys must not be logged or committed.
- Confirmation that this activity changes documentation tooling only, not Domain, CQRS handlers, persistence, migrations, or Docker persistence behavior.

No ADR, migration, runbook, or API versioning document is required unless implementation expands the decision surface.

## Open questions and approved assumptions

Approved assumptions for this spec:

- The API remains a Minimal API application.
- OpenAPI generation remains based on `Microsoft.AspNetCore.OpenApi`.
- Swagger UI may add one focused third-party UI package.
- Production should hide documentation endpoints by default, with explicit opt-in if exposing them is required.
- Examples may include the fixed evaluator credentials because the practical test requires them, but must label them as test-only.

Open questions before implementation:

- Should the production opt-in setting be named exactly `OpenApi:Enabled`, or should it follow another repository configuration naming convention?
- Should `/openapi/v1.json` remain exposed in Production for evaluator convenience, or should the stricter default of Development-only plus opt-in be enforced?
- Should Swagger UI persist authorization within the browser session if supported by the package, or should persistence be disabled for safer defaults?

## Risks

- The current `Program.cs` maps `/openapi/v1.json` unconditionally. Gating it in Production is a deliberate hardening change but may surprise anyone already relying on that endpoint outside Development.
- Swagger UI requires an additional package. Keeping only the UI package avoids replacing the existing OpenAPI generator.
- Examples can drift from DTOs if duplicated in too many places. Prefer centralized API-layer helpers or tests that assert the generated document.
- Over-documenting internals could leak implementation details. OpenAPI must describe transport DTOs and Problem Details, not EF Core entities, Domain internals, exception type names, SQL, secrets, or token values.
- The repository currently contains OpenAPI code in endpoint files. Additional metadata should remain readable and should not make route mapping difficult to explain in an interview.

## Completion evidence

When implemented later, the Orchestrator must report:

- activity branch and base branch;
- implemented acceptance-criterion map;
- files changed and unrelated changes preserved;
- package additions and API-layer OpenAPI configuration;
- Swagger UI and OpenAPI environment behavior;
- authentication/security documentation behavior;
- exact test and quality-gate commands with observed results;
- Reviewer and QA disposition;
- uncommitted changes and any authorized commits;
- suggested Conventional Commit message;
- recommended Pull Request target and next step, without creating or publishing it unless authorized.
