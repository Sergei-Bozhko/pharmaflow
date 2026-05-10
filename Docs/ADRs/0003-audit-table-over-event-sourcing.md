# ADR-0003: Audit table over event sourcing

- Status: Accepted
- Date: 2026-05-10
- Decider: project owner
- Related: PFL-030 (interceptor), PFL-032 (round-trip test), PFL-034 (this ADR)

## Context

ALCOA+ + 21 CFR Part 11 require a tamper-evident audit trail for every regulated write. Two ways to satisfy that:

1. Keep aggregates as the source of truth; emit `AuditEvent` rows as a side-effect of `SaveChangesAsync` via `SaveChangesInterceptor`.
2. Full event sourcing — events are the source of truth, aggregate state is rebuilt by replay.

## Considered Options

- Audit table via interceptor (chosen)
- Full event sourcing
- CDC / outbox to off-process event store
- Postgres `BEFORE` triggers writing to `audit_events`

## Decision

Audit table via interceptor.

## Consequences

Good:
- Aggregates queryable directly with LINQ. No replay machinery, no projections.
- Same-transaction atomicity falls out of EF (one `SaveChanges`, one tx, business + audit rows commit together).
- Audit table is a peer to business tables — `psql` works.

Bad:
- Replay-to-prior-state needs bespoke code (load `AuditEvent` rows, hydrate from `AfterStateJson`). Event sourcing gets this free; we don't have a use case for it in v1.
- DB-level append-only on `audit_events` (Postgres rules/triggers) is **not yet shipped**. App-layer only for now. Deferred to Sprint 8 with the hash chain.

## Rejected

- **Event sourcing** — disproportionate for v1. Replay infra, projections, snapshotting, event versioning all become real work. Spec §13.8 already labels it out-of-scope.
- **CDC / outbox** — adds a second runtime + eventual-consistency window. "Contemporaneous" is harder to argue when audit lands seconds behind the write.
- **Postgres triggers** — moves logic into SQL. Harder to unit-test, harder to reason about with `xmin` concurrency.

## Refs

- Spec §13, §13.5, §13.8, §10.5
- `src/PharmaFlow.Infrastructure/Persistence/Interceptors/AuditingSaveChangesInterceptor.cs`
