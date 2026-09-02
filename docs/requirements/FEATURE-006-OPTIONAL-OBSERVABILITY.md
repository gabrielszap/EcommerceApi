# FEATURE-006 - Optional Observability and Delivery Hardening

## Activity and Git context

- Classification: new activity, only after all mandatory features are complete and passing.
- Suggested branch: `chore/observability-hardening`.
- Base branch: `develop`.
- Pull Request target: `develop`.
- Depends on: `FEATURE-001` through `FEATURE-005` completed and stable.

This feature does not authorize commit, push, merge, rebase, or Pull Request creation.

## Goal

Implement only the desirable, non-eliminatory improvements that can be completed and explained confidently without destabilizing the mandatory solution.

## Priority order

1. A MediatR logging pipeline behavior using Serilog that records command/query type, outcome, and duration without exposing passwords, JWTs, signing keys, or sensitive payloads.
2. At least one API integration test with `WebApplicationFactory`.
3. Basic OpenTelemetry with console export.
4. SonarQube or `dotnet-sonarscanner` configuration in the Docker/Compose workflow.

## Included

- Only the optional item or items explicitly selected after mandatory quality gates pass.
- README documentation that marks completed optional tooling accurately.
- Tests or verification that demonstrate each selected item actually works.

## Excluded

- Starting an optional item before the mandatory API is stable.
- Logging raw login request data, credential values, JWTs, signing keys, or entire sensitive command payloads.
- Introducing distributed tracing backends, external databases, message brokers, dashboards, or CI services not required by the test.

## Acceptance criteria

- Given all mandatory quality gates have not passed, when this feature is considered, then the Orchestrator defers it.
- Given the Serilog behavior is selected, when a command or query runs, then type, outcome, and duration are logged without secrets or sensitive payloads.
- Given the integration-test item is selected, when the test suite runs, then at least one endpoint is exercised through `WebApplicationFactory` against the configured test persistence approach.
- Given OpenTelemetry is selected, when the application handles a request, then basic telemetry is exported to the console without secrets.
- Given Sonar tooling is selected, when its documented command runs, then the README describes the actual prerequisites and observed result.

## Completion evidence

Report which optional items were selected, why they were safe to add, exact verification commands and results, README changes, and any optional item intentionally deferred.
