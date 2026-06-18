# ADR-0005: Strangler-fig HTTP transport with consumer ACL + inbox

- Status: Accepted
- Date: 2026-06-18
- Decider: project owner
- Related: PFL-064 (map-at-harvest), PFL-065 (transport seam + flag), PFL-066 (consumer webhook + ACL + inbox + rollback drill). **Supersedes** the *Broker/HTTP transport*, *Map-at-harvest*, and *Consumer-side inbox* bullets of [ADR-0004](<0004-transactional-outbox.md>) § Deferred.

## Context

ADR-0004 left the cross-module event path durable but **in-process**: outbox row → processor → Mediator `INotification` → subscriber in the same process. S7's goal is to push that path across a **process/HTTP boundary** using the strangler-fig pattern — incrementally, reversibly, and *without* committing to a second deployable yet.

Three ADR-0004 deferrals come due together, and in order:

1. The outbox stored the **raw domain event** and mapped it to the integration contract on dispatch — you cannot safely ship a raw domain type over a wire, and a domain-shape change breaks replay of in-flight rows.
2. In-proc Mediator dispatch is the wrong transport for an extracted consumer.
3. Once the consumer is reached over HTTP it can no longer see the producer's `processed_on`, so it needs its own dedup.

## Considered Options

- **In-solution HTTP boundary behind a flag** (chosen) — a loopback webhook hosted in the same solution; the outbox processor POSTs over `HttpClient`; a config flag selects in-proc vs HTTP transport.
- **Separate deployable + separate database now** — a real process boundary, but two-host operational and test cost, and a real risk of a half-built second service inside the newborn + job-start-overlap budget.
- **Message broker** (RabbitMQ / Azure Service Bus) — durable cross-process transport, but a second runtime to operate and test for no S7 benefit (still one process this sprint).

## Decision Outcome

Chosen: **map-at-harvest + a pluggable transport seam behind a flag + a consumer-side ACL + inbox; in-solution, with the deployable + DB split deferred.**

- **Map-at-harvest (PFL-064).** The `SaveChanges` interceptor maps the harvested domain event to the stable `..Contracts..` integration event and stores *that* — serialized and versioned — in `outbox_messages`. Replay no longer depends on the domain type's shape; the wire payload is a contract, not a domain type. **Integration-only:** a row is written only when a domain→integration mapping exists (unmapped events still persist and still audit, untouched).
- **Transport seam + flag (PFL-065).** `IIntegrationEventDispatcher` has two implementations — `InProc` (Mediator) and `Http` (POST to the webhook). `TransportRoutingDispatcher` reads `OutboxOptions.Transport` **per call** on a DI singleton, so flipping it changes the *next* dispatch with no restart. **The flag is the rollback lever.**
- **At-least-once over HTTP.** A non-2xx throws (`EnsureSuccessStatusCode`); the processor's existing catch records a failed attempt and parks the row at the ceiling — semantically identical to an in-proc throw. No Polly or circuit breaker: the outbox attempt loop *is* the retry.
- **Consumer ACL (PFL-066).** The consumer binds to its **own** transport DTO and translates through an explicit adapter into `KnownStudy` — never to the producer's CLR contract across the wire. `RegisteredAt` is the **consumer's clock (learned-at)**, not the wire `OccurredAt`: deliberately not a 1:1 copy, so a producer-side field change is absorbed in the adapter rather than rippling into Sites. Both transports (in-proc and HTTP) now stamp learned-at, so the projection is transport-independent.
- **Consumer inbox (PFL-066).** An `inbox_messages` table keyed by **message id** is the cross-boundary replacement for the producer's `processed_on`, which the consumer can no longer see. A seen id is a no-op. The inbox row and `KnownStudy` commit in **one** `SaveChanges` — the event is durably accepted before the 2xx goes back. Belt-and-braces holds: the `KnownStudy` natural-key guard stays as the second backstop — the inbox catches a redelivered message id, the natural key catches a distinct path to the same study.
- **Effectively-once is honest now.** ADR-0004 deferred the inbox because in-proc `processed_on` plus idempotent subscribers covered dedup. Across the HTTP hop that reasoning expires — this is the deferral coming due, not a new idea.

### Consequences

Good:
- Strangler-fig **end-to-end including rollback**, proven by tests: HTTP delivery, redelivery (no double effect), and a **live flag-flip back to in-proc with no loss or duplication**.
- Rollback is a **config flip, not a redeploy or a migration** — the per-call flag read on the singleton is what makes that true.
- The ACL protects the consumer's domain; the inbox makes the consumer effectively-once *independently* of the producer.
- The seam is built so the real split is a mechanical step — point the HTTP dispatcher at a separate host — not a rewrite.

Bad:
- The webhook speaks **one contract** (a `type` guard + a single transport DTO). A second contract needs a `type`→DTO dispatch; fine at one registry entry, a known extension point.
- The inbox check-then-insert is **not atomic** under true concurrency; the message-id PK is the backstop (a lost race returns 200 after a caught `DbUpdateException`, or the producer retries and dedups). The single-instance processor makes this moot today.
- The **in-solution loopback hides** what a real split surfaces — webhook auth, network partial-failure, the second DB. Listed below so it isn't mistaken for done.

## Deferred

- **Separate deployable + separate database** — the real process/data boundary; the seam is ready for it.
- **Two-host integration harness** (or .NET Aspire) — exercise the real cross-process hop, not loopback.
- **Webhook authentication** — the `/internal/integration-events` endpoint is unauthenticated in-solution.
- **Message broker** in place of direct HTTP — durable queue, broker-side retries, dead-lettering.
- **Multi-instance processing** — `SELECT ... FOR UPDATE SKIP LOCKED` row claiming (carried from ADR-0004), which also closes the inbox concurrency window.
- **Contract version behavior** — `Version` rides in the payload but the ACL ignores it; no version-branching yet.

## Refs

- `src/PharmaFlow.Infrastructure/Outbox/IntegrationEventMap.cs` — domain→integration mapping at harvest
- `src/PharmaFlow.Application/Common/Events/IIntegrationEventDispatcher.cs` + `Infrastructure/Outbox/{InProc,Http,TransportRouting}IntegrationEventDispatcher.cs` — the transport seam
- `src/PharmaFlow.Infrastructure/Outbox/OutboxOptions.cs` — the `Transport` flag (the rollback lever)
- `src/PharmaFlow.Api/Endpoints/Internal/IntegrationEventEndpoints.cs` — the consumer webhook (type guard, inbox dedup, ACL, one-transaction commit)
- `src/PharmaFlow.Application/Modules/Sites/StudyProjection/Internal/{StudyCreatedTransportDto,StudyCreatedAcl}.cs` — the ACL; `Modules/Sites/Internal/InboxMessage.cs` — the inbox
- `tests/PharmaFlow.Tests.Integration/Consumer/IntegrationEventWebhookTests.cs` — webhook/ACL/inbox, HTTP e2e, redelivery, rollback drill; `tests/PharmaFlow.Tests.Unit/Outbox/{HttpIntegrationEventDispatcher,TransportRoutingDispatcher}Tests.cs`, `Modules/Sites/StudyCreatedAclTests.cs`
- [ADR-0004](<0004-transactional-outbox.md>) (transactional outbox — this supersedes its Broker/HTTP, Map-at-harvest, and Consumer-side-inbox deferrals), [ADR-0001](<0001-mediator-over-mediatr.md>) (Mediator as the in-proc transport)
- Fowler, *Strangler Fig Application*; Grzybek, *Modular Monolith with DDD* — outbox → broker/HTTP swap
