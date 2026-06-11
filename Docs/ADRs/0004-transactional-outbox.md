# ADR-0004: Transactional outbox with in-process dispatch

- Status: Accepted
- Date: 2026-06-11
- Decider: project owner
- Related: PFL-058 (domain-event primitive), PFL-059 (outbox write), PFL-060 (processor), PFL-061 (cross-module subscriber), PFL-062 (e2e test), PFL-063 (this ADR + arch gate)

## Context

S5 split `Application` into namespace modules (`Studies`, `Sites`) behind public `..Contracts..`, with an ArchUnitNET gate forbidding cross-module reach into `..Internal..`. The next move (S7) is to extract a module into its own service. That only works if modules already communicate **asynchronously and durably** rather than via direct in-process calls — otherwise extraction is a rewrite, not a lift.

The concrete coupling to remove: `Sites` synchronously called `IStudiesModule.StudyExistsAsync` to validate a study. Across a process boundary that becomes a fragile cross-service call. The replacement is event-driven — `Studies` raises `StudyCreated`, `Sites` keeps a local read-model (`KnownStudy`) fed by that event.

The problem this ADR settles: **how does an event reliably leave the producer?** A naive "save the aggregate, then publish" has a window where the save commits and the publish is lost (crash, broker down) — the modules silently diverge. ALCOA+/Part 11 makes silent divergence unacceptable.

## Considered Options

- **Transactional outbox, in-proc dispatch** (chosen) — an EF `SaveChanges` interceptor writes event rows to an `outbox_messages` table in the *same transaction* as the aggregate; a background processor polls, dispatches in-proc via Mediator `INotification`, marks rows processed.
- **Direct in-proc publish** in the handler (`IPublisher.Publish` after `SaveChanges`) — no table, but no atomicity: the publish can be lost after the commit.
- **Message broker** (RabbitMQ / Azure Service Bus) with an outbox feeding it — durable cross-process transport, but a second runtime to operate and test for no S6 benefit (still one process).
- **Exactly-once delivery** — distributed transactions / dedup protocol over the broker.

## Decision Outcome

Chosen: **transactional outbox, in-process Mediator dispatch, at-least-once delivery, belt-and-braces idempotency.**

- **Atomic capture.** The outbox row and the aggregate commit in one transaction (one `SaveChanges`). Either both land or neither does — proven by the PFL-062 rollback test (force a failure mid-transaction; assert neither row survives). This is the whole point: the event cannot be lost relative to the state change that produced it.
- **At-least-once, not exactly-once.** The processor may crash after a subscriber's side effect but before it writes `processed_on`; on restart it re-delivers. Exactly-once across a crash boundary needs distributed coordination we don't want. At-least-once + idempotent consumers is the honest, standard trade.
- **Belt-and-braces idempotency — and why both.** (1) The processor skips rows already marked `processed_on` (the common case — cheap dedupe). (2) Subscribers are independently idempotent (the `StudyCreated` handler checks `KnownStudy` by natural key before inserting). The two cover *different* failure modes: processor dedupe handles a re-poll of an already-finished row; subscriber idempotency handles a replay where the side effect succeeded but the processed-write didn't. Neither alone closes the crash window.
- **In-proc dispatch, no broker.** The outbox row is the durability boundary; Mediator `INotification` is just the in-process transport. Swapping to a broker later is a transport change, not a redesign.
- **Single-instance assumption.** One processor instance, sequential batch — so the subscriber's existence-check-then-insert has no concurrent writer, and the natural-key PK is the DB backstop.

### Consequences

Good:
- Events cannot be lost relative to the state that produced them — atomicity falls out of one EF transaction (same property as ADR-0003's audit rows).
- Modules decouple: `Sites` reads its own `KnownStudy` projection instead of calling `Studies` — the seam S7 extraction needs.
- All in one process / one DB — no broker to run, nothing new in CI; Testcontainers Postgres proves the real semantics.
- The boundary stays enforced: integration events live in `..Contracts..`, subscribers reach the producer only via contracts (ArchUnitNET R6/R7, PFL-063).

Bad:
- At-least-once pushes idempotency onto every subscriber — a standing rule, not a one-off. Documented here so it isn't forgotten when the next subscriber lands.
- Polling has latency (`PollInterval`) and DB load; fine at S6 volume, revisit with `LISTEN/NOTIFY` or a broker if it bites.
- The domain event is stored raw in the outbox and mapped to the integration contract on dispatch — a payload-shape change could break replay of in-flight rows. Map-at-harvest (store the stable contract) is the more robust evolution.

## Deferred to S7+

- **Broker / HTTP transport** — replace in-proc Mediator dispatch once a module is physically extracted.
- **Multi-instance processing** — `SELECT ... FOR UPDATE SKIP LOCKED` row claiming, so the single-instance assumption can be dropped.
- **Schema-per-module outbox** — today one `outbox_messages` over the single `AppDbContext`.
- **Map-at-harvest integration contract** — serialize the stable cross-module contract into the outbox instead of the raw domain event, decoupling replay from domain-type churn.

## Refs

- `src/PharmaFlow.Infrastructure/Persistence/Interceptors/OutboxSaveChangesInterceptor.cs` — same-transaction capture
- `src/PharmaFlow.Infrastructure/Outbox/OutboxProcessor.cs` — poll / dispatch / mark / retry
- `src/PharmaFlow.Application/Modules/Sites/StudyProjection/Internal/StudyCreatedHandler.cs` — idempotent subscriber
- `tests/PharmaFlow.Tests.Integration/Outbox/OutboxEndToEndTests.cs` — atomicity + at-least-once replay
- ADR-0001 (Mediator — the in-proc transport), ADR-0003 (same-transaction side-effect precedent)
- Grzybek, *Modular Monolith with DDD* — outbox + domain/integration event split
