# PharmaFlow

> Modular monolith for regulated workloads — portfolio simulation on .NET 10 + Azure.

[![CI](https://github.com/Sergei-Bozhko/pharmaflow/actions/workflows/ci.yml/badge.svg)](https://github.com/Sergei-Bozhko/pharmaflow/actions/workflows/ci.yml)
![License](https://img.shields.io/badge/license-MIT_(tbc)-lightgrey)

## What this is

PharmaFlow is a **modular monolith for regulated workloads**, built to demonstrate the modernization patterns such systems lean on, on a modern .NET 10 + Azure stack:

- **Enforced module boundaries** — public contracts + `internal` handlers + narrow per-module persistence interfaces, policed by an ArchUnitNET build gate (not just a code-review convention).
- **CQRS pipeline** — Logging → Validation → Idempotency → Audit → Transaction behaviors over a source-generated Mediator.
- **Typed-error `Result<T>`** over exceptions for expected failures; **idempotent commands** keyed off an `Idempotency-Key` header.
- **Tamper-evident audit trail** — hash-chained, written by an EF Core `SaveChangesInterceptor`.

The concrete domain is **clinical trials (21 CFR Part 11 / ALCOA+)** — one example of a regulated domain where audit, idempotency, and separation of concerns are non-negotiable; the same controls map onto financial-services workloads (SOX-style audit, idempotent money movement). It's a portfolio simulation, not a real product: no QMS, no validated SOPs, no formal IQ/OQ/PQ. Project 1 of 3 in a 6-month rebuild aimed at a senior .NET modernization role.

The 12-week build is divided into 12 one-week sprints. Sprint plan and per-sprint tickets live under `Planning/` (git-ignored — internal working docs).

## What this is NOT

The list below is what I deliberately did *not* build. Each cut was the right one for a 12-week budget — pharma simulators that try to do everything tend to do nothing well.

- **Real EDC (CRF capture, edit checks).** Veeva Vault and Medidata Rave own this vertical.
- **Real PII / PHI handling.** Subject pseudonyms only. Avoids GDPR/HIPAA scope.
- **HL7 / FHIR / EHR / LIMS / IRT / safety reporting (E2B).** Separate domains, separate projects.
- **Subject-facing portal / eConsent app.** Coordinator drives consent capture.
- **Real regulatory submission (eCTD, ESG).**
- **Full validation package (URS / FS / IQ / OQ / PQ).** One representative trace-matrix entry only.
- **Multi-tenant SaaS, billing, org hierarchies.** Single-tenant; multi-tenant *seam* (TenantId shadow + global filter) only.
- **Microservices, event sourcing, Kubernetes, Service Bus, Kafka, GraphQL, gRPC.**
- **Custom OAuth (Entra External ID / Auth0 / Okta).** ASP.NET Core Identity + custom JWT for v1; swap path documented.
- **AutoMapper, Quartz, Camunda, Elsa, Redis.** Mapperly, `BackgroundService`, hand-rolled state machines, in-memory cache.
- **Bicep / Terraform IaC.** `az` CLI scripts only for v1.
- **Real AV upload scanning.** Quarantine pattern as placeholder.
- **Multi-region failover, VNet, WAF, Front Door, CMK Key Vault Premium.**
- **Internationalisation.** English, UTC + one display TZ.

Full list with rationale: see [Technical Specification §6](<Docs/PharmaFlow — Technical Specification.md>).

## Stack

- **.NET 10** (SDK pinned in `global.json`)
- **EF Core 10** + **PostgreSQL** — Azure DB for PostgreSQL Flexible Server in prod; Postgres in Docker locally
- **Mediator** (`martinothamar/Mediator`) — source-generated, MIT. Replaces MediatR (commercial as of v12). See [ADR-0001](<Docs/ADRs/0001-mediator-over-mediatr.md>).
- **FluentValidation**
- **Mapperly** (source generator)
- **ASP.NET Core Identity + custom JWT** issuer
- **Blazor Web App** (Server interactivity)
- **Azure App Service / Key Vault / Blob Storage / Application Insights**
- **Serilog + OpenTelemetry** → App Insights via OTLP
- **xUnit v3** + **Testcontainers** + **FluentAssertions** + **NSubstitute** + **ArchUnitNET** (module-boundary gate)

## Architecture

Clean Architecture across seven projects: `Domain` (pure C# aggregates and value objects, no framework references), `Application` (CQRS commands / queries / handlers / pipeline behaviors), `Infrastructure` (EF Core, Blob adapter, JWT issuer, OTel wiring), `Api` (Minimal API endpoint groups, composition root), `Web` (Blazor Server), plus unit, integration, and architecture test projects. Source-code dependencies point inward; runtime adapters are registered in DI from the composition root.

Within `Application`, code is organized into **modules** (`Modules/{Studies,Sites}`). Each module exposes a public contract (`IStudiesModule`) and keeps its handlers, module-impl class, and a narrow per-module `DbContext` interface (`IStudiesDbContext`, over the single `AppDbContext`) inside its `Internal` namespace. Cross-module calls go through contracts only — never a direct EF join or another module's persistence interface. The boundary is verified by an ArchUnitNET test gate (`tests/PharmaFlow.Tests.Architecture`) that fails the build on a violation.

Full layer map and dependency rules: see [Technical Specification §7](<Docs/PharmaFlow — Technical Specification.md>) and [Docs/Architecture/03-module-dependencies](<Docs/Architecture/03-module-dependencies.md>).

### How the boundaries are enforced (30-second version)

Three mechanisms, increasing in teeth: (1) namespace + `internal` visibility keep handlers and per-module persistence out of another module's reach; (2) cross-module access is funnelled through public `..Contracts..` interfaces; (3) an ArchUnitNET test in CI fails the build if a module references another module's `..Internal..` types or DbContext.

Cross-module **communication** is event-driven, not direct calls: a producer raises a domain event, an EF interceptor writes it to an outbox table **in the same transaction** as the aggregate, and a background processor dispatches it in-proc (Mediator `INotification`) to a subscriber in another module. Delivery is **at-least-once** with **idempotent consumers** — a re-delivered event produces no duplicate effect. That is what makes a module extractable later: atomic event capture + at-least-once delivery + idempotent consumers survive a physical split unchanged. Event contracts live in `..Contracts..` and subscribers reach the producer only through them, enforced by the same arch gate (rules R6/R7); see [ADR-0004](<Docs/ADRs/0004-transactional-outbox.md>).

Deliberately **deferred to later sprints**: separate module assemblies (S7), schema-per-module DbContexts (S6/S7), and a message broker / HTTP transport in place of in-proc dispatch (S7). The honest story is *namespace modules + contracts + arch gate + in-proc outbox now, physical extraction next* — not "microservices-ready." The incremental, in-place modularization is lower-risk than a premature physical split, and is itself the talking point.

## Run locally

Prerequisites:

- **.NET 10 SDK** (version pinned in `global.json`)
- **Docker** — for PostgreSQL via `docker-compose`, and Azurite for Blob Storage emulation (Sprint 9)
- **`gh` CLI** (optional — for PR / Actions interaction from the terminal)

### Dev DB

Local PostgreSQL runs in Docker. Start it once per session:

```bash
docker-compose up -d postgres
```

The design-time connection string (used by `dotnet ef` CLI; runtime DI is wired separately in Sprint 4) is read from .NET User Secrets first, then falls back to the `PHARMAFLOW_DEV_CONNECTION` environment variable. Set it once per machine:

```bash
dotnet user-secrets set "PHARMAFLOW_DEV_CONNECTION" \
    "Host=localhost;Port=5432;Database=pharmaflow_dev;Username=pharmaflow;Password=pharmaflow" \
    --project src/PharmaFlow.Infrastructure
```

Stored in `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json` — outside the repo, never committed. Override per-shell (CI, alternate creds) by exporting the env var; it wins over user secrets:

```bash
export PHARMAFLOW_DEV_CONNECTION='Host=...;Port=...;Database=...;Username=...;Password=...'
```

Stop and wipe the DB volume (rare; only for a clean slate):

```bash
docker-compose down -v
```

### Migrations (PFL-029 onwards)

```bash
dotnet ef migrations add <Name> \
    --project src/PharmaFlow.Infrastructure \
    --startup-project src/PharmaFlow.Api
```

Migration `.cs` files land under `src/PharmaFlow.Infrastructure/Persistence/Migrations/` and are committed to git. CI builds them like any other code; CI does not run `dotnet ef`.

### Continuous integration

CI runs ordered steps on the same `ubuntu-latest` runner:

- **Architecture** — `dotnet test --project tests/PharmaFlow.Tests.Architecture` (ArchUnitNET module-boundary rules; no DB, no Docker, fast — runs right after build so a boundary break fails early).
- **Unit** — `dotnet test --project tests/PharmaFlow.Tests.Unit` (no DB; no Docker required).
- **Integration** — `dotnet test --project tests/PharmaFlow.Tests.Integration`. Testcontainers spins up an ephemeral Postgres container per test session against the runner's pre-installed Docker daemon. ~30 s cold-start on first run; image cached afterwards.

Project-scoped over trait-filtered (`--filter-trait`) — avoids MTP exit-code-8 when one project has zero matches. Trait `[Trait("Category", "Integration")]` is still applied to the integration base class for local iteration (`dotnet test --filter-trait "Category=Integration"`).

## Compliance

PharmaFlow is a **portfolio simulation, not validated software.** There is no quality management system, no IQ/OQ/PQ protocols, no signed-off SOPs, no supplier audit, no periodic review. What it does demonstrate is the technical-control surface a Part-11-aligned system needs: hash-chained audit trail via an EF Core `SaveChangesInterceptor`, two-component electronic signatures with continuous-session handling, separation-of-duty roles enforced at both application and database principal level, and immutability of signed records.

Honest framing: the project proves I can implement the controls correctly and reason about where the simulation ends. Full mapping (21 CFR Part 11 Subparts B and C, ALCOA+, GAMP 5): see [Technical Specification §20](<Docs/PharmaFlow — Technical Specification.md>).

## Documents

| Document | Path |
|---|---|
| Technical Specification | [`Docs/PharmaFlow — Technical Specification.md`](<Docs/PharmaFlow — Technical Specification.md>) |
| Architecture Decision Records | [`Docs/ADRs/`](Docs/ADRs/) |
| Architecture diagrams (C4) | [`Docs/Architecture/`](Docs/Architecture/) |

## License

MIT — to be confirmed before public showcase.
