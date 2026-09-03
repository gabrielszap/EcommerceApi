# FEATURE-ID — Feature name

## Activity and Git context

- Activity classification: new activity | continuation
- Suggested branch: `<type>/<short-kebab-case-description>`
- Base branch: `develop` | `main` only for an explicitly authorized `hotfix/*`
- Expected Pull Request target: `develop` | `main` only for production promotion or authorized hotfix
- Dependency on another activity or branch: none | describe the explicitly approved dependency

The Engineering Orchestrator must validate this information against the repository state and the Git policy in the root `AGENTS.md`. For a new normal activity, it must create or reuse `develop` according to the first-time bootstrap rule and then create the activity branch before any file-changing work. For a continuation, it must reuse the current activity branch when its scope remains accurate.

This requirement file does not authorize commits, push, merge, rebase, branch deletion, Pull Request creation, or release publication. Those operations require an explicit user request.

## Goal

Describe the user or business outcome in one paragraph.

## Context

Explain the current behavior, actors, relevant domain terms, and why the change is needed.

## Scope

### Included

- List behavior that must be delivered.

### Excluded

- List adjacent behavior that must not be added.

## Architecture and affected areas

Identify the expected Domain, Application, Infrastructure, API, test, documentation, Docker, migration, and CI/CD impact. State which areas must remain unchanged. The Architect validates these boundaries before implementation; this section does not authorize unrequested abstractions or dependencies.

## API or event contract

Define method/route or event name/topic, authentication/authorization, request or payload, successful response/outcome, headers, and error outcomes.

## Business rules

1. State each rule in observable terms.
2. Define normalization, time window, status, ordering, and ownership semantics explicitly.
3. State concurrency and duplicate behavior.

## Persistence and consistency

Describe data changes, constraints, transaction boundary, migration/backfill needs, and database/event coordination.

## Security and privacy

Define permissions, tenant/resource ownership, sensitive fields, retention, and audit needs.

## Failure behavior

Define validation, conflict, dependency failure, timeout, cancellation, retry, and recovery behavior.

## Observability

Define useful structured logs, metrics, traces, alerts, correlation, and health impact without exposing sensitive data.

## Compatibility and rollout

Describe client/consumer compatibility, versioning, deployment order, feature flags, rollback, and data migration risks.

## Acceptance criteria

- Given a precise starting state, when an action occurs, then an observable outcome follows.
- Include happy path, rejection, authorization, concurrency/duplicate, and dependency-failure criteria when relevant.

## Test and verification matrix

- Domain tests:
- Command/query handler tests:
- Validation pipeline tests:
- PostgreSQL/Dapper integration tests:
- API integration tests:
- Docker/Compose verification:
- Security and authorization checks:
- Required quality gates:

Use the cheapest reliable test level for each acceptance criterion. Every command and query handler requires automated tests. Persistence behavior must be validated against PostgreSQL rather than an in-memory replacement.

## Documentation and operational impact

List required updates to README, OpenAPI, requirements, ADRs, migrations, runbooks, environment examples, Docker/Compose behavior, or deployment guidance. Do not document optional behavior as completed until it has been implemented and verified.

## Open questions and approved assumptions

Record unresolved decisions and explicitly approved assumptions with owner/date when available.

Material ambiguity must be resolved before code changes. Do not convert an unanswered business-critical question into an implicit implementation decision.

## Completion evidence

The Orchestrator must report:

- activity branch and base branch;
- implemented requirement and acceptance-criterion map;
- files changed and unrelated changes preserved;
- migrations, authentication, persistence, Docker, and documentation impact when applicable;
- exact test and quality-gate commands with observed results;
- Reviewer and QA disposition;
- uncommitted changes and any authorized commits;
- suggested Conventional Commit message;
- recommended Pull Request target and next step, without creating or publishing it unless authorized.
