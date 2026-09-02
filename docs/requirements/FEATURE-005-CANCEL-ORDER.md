# FEATURE-005 - Cancel Order

## Activity and Git context

- Classification: new activity.
- Suggested branch: `feature/cancel-order`.
- Base branch: `develop`.
- Pull Request target: `develop`.
- Depends on: `FEATURE-003-CREATE-ORDER.md`.

This feature does not authorize commit, push, merge, rebase, or Pull Request creation.

## Goal

Provide an authenticated cancellation operation that delegates the status transition to the Order aggregate and persists the result atomically.

## Included

- `PATCH /api/orders/{id}/cancel` implemented through a MediatR command.
- Domain cancellation behavior that allows only `Pending -> Cancelled`.
- EF Core persistence of the updated status.
- Unit tests for the command handler and Domain transition.
- SQLite integration coverage for persisted cancellation behavior when practical.
- OpenAPI and README documentation for success, not-found, and invalid-state outcomes.

## Excluded

- An endpoint to confirm an order.
- Cancelling individual items, deleting orders, refunds, stock restoration, audit history, or cancellation reasons.
- Generic repository abstractions or client-selected status updates.

## HTTP contract

### `PATCH /api/orders/{id}/cancel`

Requires a valid bearer token and has no request body.

Successful response: `200 OK` with the cancelled order representation, including `status: "Cancelled"` and calculated `totalAmount`.

Malformed GUID: `400 Bad Request`.

Unknown valid GUID: `404 Not Found`.

Order currently `Cancelled` or `Confirmed`: `409 Conflict` using Problem Details, because the requested state transition is not allowed.

## Domain and persistence rules

1. `Order.Cancel()` is the only behavior that performs the cancellation transition.
2. It changes `Pending` to `Cancelled`.
3. It must reject or return an explicit domain outcome for any status other than `Pending`.
4. The command handler loads the aggregate, invokes Domain behavior, and persists the aggregate through Infrastructure.
5. The API endpoint and handler must not set the status directly.
6. The transition and persistence are atomic.

## Acceptance criteria

- Given an authenticated request for an existing pending order, when cancellation is requested, then it returns `200` and the stored status becomes `Cancelled`.
- Given an order already cancelled, when cancellation is requested again, then it returns `409` and preserves the existing state.
- Given a confirmed order, when cancellation is requested, then it returns `409` and preserves the existing state.
- Given an unknown valid order ID, when cancellation is requested, then it returns `404`.
- Given a malformed ID, when cancellation is requested, then it returns `400`.
- Given no valid bearer token, when cancellation is requested, then authorization rejects it.

## Verification matrix

- Domain tests: pending cancellation succeeds; cancelled and confirmed orders reject cancellation.
- Handler tests: success, not found, and invalid status outcome mapping.
- SQLite integration test: cancellation persists and survives a new read context.
- API check: `200`, `400`, `404`, `409`, and authorization behavior.

## Completion evidence

Report the transition behavior, response contract, persistence evidence, handler and Domain test results, and confirmation that no layer outside Domain assigns the cancelled state.
