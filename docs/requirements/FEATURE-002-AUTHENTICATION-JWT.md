# FEATURE-002 - In-Memory Login and JWT Authentication

## Activity and Git context

- Classification: new activity.
- Suggested branch: `feature/authentication-jwt`.
- Base branch: `develop`.
- Pull Request target: `develop`.
- Depends on: `FEATURE-001-PROJECT-BOOTSTRAP.md`.

This feature does not authorize commit, push, merge, rebase, or Pull Request creation.

## Goal

Provide the anonymous login endpoint required by the test and protect every order route with JWT bearer authentication, using only the fixed in-memory evaluator credentials.

## Included

- `POST /auth/login` implemented as a MediatR command.
- Login request validation through FluentValidation in the MediatR pipeline.
- Fixed in-memory credential comparison for exactly `dev@martech.com` and `Senha@123`.
- JWT generation through an Application-owned abstraction with Infrastructure implementation.
- JWT bearer validation configured in the API project.
- Authorization applied to every `/api/orders` route.
- Unit tests for the login handler, validation behavior, and JWT-related success/failure behavior at the appropriate boundary.
- README and OpenAPI documentation for login and bearer-token use.

## Excluded

- A User database table, `DbSet<User>`, user migration, registration endpoint, password reset, refresh token, OAuth, or password hash.
- Claims and roles beyond the minimum needed to issue and validate a token for this API.
- Logging credential values, the signing key, or full JWT values.

## HTTP contract

### `POST /auth/login`

The endpoint is anonymous.

Request body:

```json
{
  "email": "dev@martech.com",
  "password": "Senha@123"
}
```

Successful response: `200 OK` with a JSON body that contains at least `accessToken` and an expiration value that can be consumed by API clients.

Invalid request shape or missing required values: `400 Bad Request` using the repository Problem Details convention.

Incorrect email or password: `401 Unauthorized` using one consistent failure response.

## JWT contract

- Issuer, audience, token lifetime, and signing key come from configuration.
- The signing key is supplied through environment or untracked local configuration; the repository contains only a safe placeholder/example.
- The token is valid for the configured issuer, audience, signature, and lifetime.
- Order endpoints require `Authorization: Bearer <accessToken>`.
- Missing, malformed, invalid, or expired tokens are rejected by the configured bearer middleware.

## Architecture contract

The endpoint forwards the request to a login command. The command handler validates the fixed credentials through a small Application-owned interface or focused service and requests a token from an Application-owned token generator port. The API endpoint does not compare credentials or manually construct a JWT.

The fixed credentials are a test fixture. The README must state that this design is intentionally not suitable for production identity management.

## Acceptance criteria

- Given the required email and password, when `POST /auth/login` is called, then it returns `200 OK` and a JWT accepted by a protected order route.
- Given an incorrect email or password, when login is called, then it returns `401 Unauthorized` and no token.
- Given a request missing email or password, when login is called, then FluentValidation produces the configured `400` validation response.
- Given no bearer token, a malformed token, an invalid signature, or an expired token, when any `/api/orders` route is called, then the request is rejected.
- Given the codebase, when inspected, then no credential source is persisted through EF Core or SQLite.
- Given logs and configuration, when inspected, then they contain neither the submitted password, the signing key, nor full issued JWTs.

## Verification matrix

- Unit tests: valid credentials, invalid email, invalid password, and validation failures for the login command handler.
- Pipeline test: validation executes through MediatR rather than endpoint-only logic.
- API check: obtain a token and call a protected route with and without it.
- Configuration check: use a safe test signing key only through local/environment configuration.

## Completion evidence

Report the JWT configuration keys without exposing their values, the request/response contract, unit-test results, protected-route evidence, and README/OpenAPI updates.
