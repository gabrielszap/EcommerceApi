# FEATURE-004 - Query Orders and Pagination

## Activity and Git context

- Classification: new activity.
- Suggested branch: `feature/query-orders`.
- Base branch: `develop`.
- Pull Request target: `develop`.
- Depends on: `FEATURE-003-CREATE-ORDER.md`.

This feature does not authorize commit, push, merge, rebase, or Pull Request creation.

## Goal

Expose authenticated read endpoints for a paginated list of orders and an individual order, with deterministic ordering and no leakage of EF Core concerns into the API contract.

## Included

- `GET /api/orders?page=1&pageSize=10` implemented as a MediatR query.
- `GET /api/orders/{id}` implemented as a MediatR query.
- EF Core read queries in Infrastructure with explicit deterministic ordering.
- DTO mapping that exposes an order representation without exposing EF Core entities.
- Unit tests for both query handlers.
- SQLite integration coverage for persisted read behavior when practical.
- OpenAPI and README documentation for both endpoints and pagination.

## Excluded

- Filtering, search, sorting selected by the client, cursor pagination, exports, reporting, caching, or customer authorization rules not required by the test.
- Mutation of orders or item collections.

## HTTP contract

### `GET /api/orders?page=1&pageSize=10`

Requires a valid bearer token.

- `page` defaults to `1` when omitted.
- `pageSize` defaults to `10` when omitted.
- Both values must be positive integers; invalid values return `400 Bad Request` using Problem Details.
- Results use deterministic ordering: newest `createdAt` first, with `id` as a stable tie-breaker.

Successful response: `200 OK` with an envelope containing at least `items`, `page`, `pageSize`, and `totalCount`. Each item includes the order fields needed by consumers, including calculated `totalAmount`.

### `GET /api/orders/{id}`

Requires a valid bearer token.

Successful response: `200 OK` with `id`, `customerId`, `status`, `createdAt`, `items`, and calculated `totalAmount`.

Malformed GUID: `400 Bad Request`.

Unknown valid GUID: `404 Not Found` using Problem Details.

## Query behavior

- Queries do not mutate the aggregate or database state.
- Total amount shown in DTOs reflects the Domain-defined item subtotal formula; query projection may display the value but must not become an alternative business-rule source.
- Query handlers accept and propagate `CancellationToken`.
- Pagination is applied at the database query level, not after reading every row into memory.

## Acceptance criteria

- Given several persisted orders, when the first list page is requested, then it returns `200`, page metadata, at most `pageSize` items, and deterministic newest-first order.
- Given a second page, when it is requested, then it returns the correct non-overlapping slice and the same `totalCount` semantics.
- Given invalid `page` or `pageSize`, when the list is requested, then it returns `400`.
- Given an existing order identifier, when it is requested, then it returns its persisted order and items with the correct total.
- Given a missing identifier, when it is requested, then it returns `404`.
- Given no valid bearer token, when either route is requested, then authorization rejects it.

## Verification matrix

- Handler tests: default pagination, explicit pagination, invalid pagination, existing order, and missing order.
- SQLite integration test: persist multiple orders and verify ordering, page boundaries, included items, and total count.
- API check: `200`, `400`, `404`, and authorization behavior.

## Completion evidence

Report the pagination envelope, deterministic ordering rule, query-handler tests, integration evidence, and README/OpenAPI updates.
