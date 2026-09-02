# FEATURE-003 - Create Order

## Activity and Git context

- Classification: new activity.
- Suggested branch: `feature/create-order`.
- Base branch: `develop`.
- Pull Request target: `develop`.
- Depends on: `FEATURE-001-PROJECT-BOOTSTRAP.md` and `FEATURE-002-AUTHENTICATION-JWT.md`.

This feature does not authorize commit, push, merge, rebase, or Pull Request creation.

## Goal

Create a protected endpoint that persists a valid order while enforcing every order-construction invariant in the Domain layer.

## Included

- `POST /api/orders` protected by JWT bearer authentication.
- A create-order command and handler using MediatR.
- Domain construction behavior for `Order` and `OrderItem` that enforces the required invariants.
- EF Core persistence for a newly created order and its items.
- FluentValidation for request-shape validation, while the Domain remains the source of truth for business invariants.
- Unit tests for the command handler and Domain behavior.
- OpenAPI and README documentation for the create-order request and response.

## Excluded

- Product catalog lookup, stock validation, price calculation from external data, payment, order confirmation, customer persistence, discounts, or idempotency keys.
- Updates to an existing order or item.
- A generic repository abstraction.

## HTTP contract

### `POST /api/orders`

Requires a valid bearer token.

Request body:

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

Successful response: `201 Created`, a `Location` header for `/api/orders/{id}`, and an order representation containing at least `id`, `customerId`, `status`, `createdAt`, `items`, and calculated `totalAmount`.

Invalid input or violated order invariant: `400 Bad Request` using Problem Details. Validation responses must identify the invalid field or business condition according to the repository convention.

## Domain rules

1. A new order has a generated `Guid` identifier, the provided `CustomerId`, the current UTC creation time, and status `Pending`.
2. An order with zero items is invalid.
3. Each item has a generated `Guid`, belongs to the created order, and has the supplied product name, quantity, and unit price.
4. Quantity less than or equal to zero is invalid.
5. Unit price less than or equal to zero is invalid.
6. `TotalAmount` is a Domain-calculated value: sum each `Quantity * UnitPrice` using decimal arithmetic.

The handler orchestrates creation and persistence; it must not reproduce the total calculation or cancellation logic.

## Persistence contract

The handler persists one aggregate and its items through Infrastructure/EF Core. The write is atomic: a failed item persistence must not leave a partial order. EF Core mapping remains in Infrastructure and no `DbContext` is exposed to API or Domain.

## Acceptance criteria

- Given a valid authenticated request with one or more valid items, when the endpoint is called, then it returns `201 Created` and the order can be retrieved later with the same items and total.
- Given no items, non-positive quantity, or non-positive unit price, when an order is created, then it returns `400` and no order is persisted.
- Given multiple items, when the order is created, then `totalAmount` equals the decimal sum of all item subtotals.
- Given a valid request without a bearer token, when the endpoint is called, then it is rejected by authorization.
- Given the Domain tests, when invalid construction is attempted outside the API, then the aggregate prevents invalid state.

## Verification matrix

- Domain unit tests: one item, multiple items, zero items, zero/negative quantity, zero/negative unit price, and total calculation.
- Handler unit tests: valid persistence, persistence failure behavior, and expected response mapping.
- SQLite integration test: persisted order and items can be read with their relationship intact.
- API check: authenticated `201 Created`, unauthenticated rejection, and invalid request `400`.

## Completion evidence

Report the command, handler, Domain rules, migration impact if any, test evidence, response example, and confirmation that total calculation remains inside Domain behavior.
