# ADR-0001: Mediator over MediatR

- Status: Accepted
- Date: 2026-04-29
- Decider: project owner
- Related: PFL-006 (CPM swap landed in lockfile), PFL-013 (this ADR + spec amendments)
- Supersedes: spec §8 (Mediator row prior wording), spec §29 (prior ADR-0002 slot)

## Context and Problem Statement

MediatR moved to a commercial license starting with v12.x — paid per-seat for production use. PharmaFlow is a portfolio repository with no licensing budget, so the originally specced default (`MediatR 12.4.x`, spec §8) is no longer viable without cost.

Spec §23.2 already anticipated this risk in the original wording: *"swap to `Mediator` (martinothamar) is a 2-hour mechanical refactor if licensing changes for v13+"*. The license change happened a major version earlier than the spec expected, so the swap moves from contingency to v1 default.

## Considered Options

* Replace with **`Mediator`** (martinothamar/Mediator) — source-generated, MIT-licensed
* Pin **MediatR 11.x** — last free release
* Pay for **MediatR 12+** commercial license
* **Hand-rolled** mediator (~150 lines for `IRequest<T>` dispatch + pipeline behaviors)
* **FastEndpoints** — Minimal-API-shaped library, replaces the handler model

## Decision Outcome

Chosen option: **"Replace with `Mediator`"**, because it is free, MIT-licensed, source-generated (zero-allocation dispatch — faster than reflection-based MediatR), and keeps the handler/`ISender`/`IPublisher` surface near-identical to MediatR.

Rejected:
- *MediatR 11.x* — pins the project to an EOL line and forfeits future fixes.
- *MediatR 12+ commercial* — no licensing budget allocated for a portfolio repo.
- *Hand-rolled* — reinvents a solved problem; sprint capacity better spent on domain code.
- *FastEndpoints* — different mental model (endpoints, not handlers); would force a rewrite of the CQRS plan in §9.2.

### Consequences

* Good — source-gen → zero-allocation dispatch; faster than reflection-based MediatR.
* Good — free, MIT, no per-seat license.
* Good — `ISender`/`IPublisher`/`IRequest<T>` surface near-identical to MediatR; handler code is largely portable.
* Bad — pipeline-behavior registration syntax differs from MediatR's `services.AddMediatR(...)`; document the wiring once in §9.2 when first handler lands (Sprint 2).
* Bad — smaller community than MediatR; fewer Stack Overflow hits, niche edge cases may require reading the source.
* Bad — documentation skewed toward a single maintainer; bus-factor risk if the project goes unmaintained. Mitigation: source is small (< 5 kLOC); fork is feasible.

## Sources / Links

- Spec: §8 (Stack Picks — Mediator row), §9.2 (Application-layer CQRS), §23.2 (CPM lockfile)
- Tickets: PFL-006 (CPM swap implementation), PFL-013 (this ADR + spec amendments)
- Library: <https://github.com/martinothamar/Mediator>