# 02 — Container Diagram (C4 Level 2)

Zooms one level inside the PharmaFlow box from doc 01: the deployable / runnable units, how a request flows, and where the auth boundary sits. Source: spec §7.1 (project structure), §9 (layered patterns), §11 (auth).

```mermaid
flowchart LR
    classDef person fill:#08427b,stroke:#073b6f,color:#fff
    classDef container fill:#438dd5,stroke:#2e6295,color:#fff,stroke-width:2px
    classDef domain fill:#85bbf0,stroke:#5d82a8,color:#000,stroke-width:2px
    classDef datastore fill:#999,stroke:#6b6b6b,color:#fff
    classDef external fill:#bbb,stroke:#888,color:#222

    User["👤 User<br/>(any of 5 roles)"]:::person

    subgraph PharmaFlow["PharmaFlow system"]
        direction TB
        Web["🌐 PharmaFlow.Web<br/>Blazor Web App (Auto, .NET 10)<br/>cookie auth (Identity)"]:::container
        Api["🔌 PharmaFlow.Api<br/>Minimal API (.NET 10)<br/>JWT bearer + X-Api-Key"]:::container
        App["⚙️ PharmaFlow.Application<br/>CQRS handlers + Mediator pipeline<br/>(Logging → Validation → Idempotency<br/>→ Transaction → Audit)"]:::container
        Domain["💎 PharmaFlow.Domain<br/>Aggregates, VOs, typed IDs,<br/>Result/Error, domain events.<br/>Zero framework deps."]:::domain
        Infra["🔧 PharmaFlow.Infrastructure<br/>EF Core 10, AppDbContext,<br/>repos, AuditingSaveChangesInterceptor,<br/>Blob/KV/JWT/Clock adapters"]:::container
    end

    Postgres[("🗄 PostgreSQL<br/>EF migrations + xmin concurrency")]:::datastore
    Blob[("🗂 Azure Blob<br/>WORM containers")]:::datastore
    KV[("🔐 Key Vault")]:::external

    User -->|"HTTPS<br/>browser"| Web
    User -->|"HTTPS<br/>service-to-service"| Api

    Web -->|"in-process<br/>(Auto interactivity)"| App
    Api -->|"ISender.Send(...)"| App

    App -->|"interfaces only<br/>(IAppDbContext,<br/>IStudyRepository,<br/>ICurrentUser, IClock)"| Domain
    App -->|"resolves ports<br/>via DI"| Infra

    Infra -->|"implements<br/>Domain interfaces"| Domain
    Infra -->|"npgsql"| Postgres
    Infra -->|"Azure SDK"| Blob
    Infra -->|"Azure SDK<br/>(Sprint 8+)"| KV
```

## Request flow (write path)

1. User submits form in Blazor Web → component calls API endpoint over HTTPS (or in-process during Auto Server-prerender).
2. `PharmaFlow.Api` endpoint group authenticates JWT, captures `Idempotency-Key` + `correlationId`, calls `ISender.Send(command)`.
3. Mediator pipeline runs **outer → inner**: `LoggingBehavior` → `ValidationBehavior` (FluentValidation, short-circuits) → `IdempotencyBehavior` (24h-cached responses) → `TransactionBehavior` (opens EF tx) → `AuditBehavior` (high-level audit row) → handler.
4. Handler: pulls aggregate via repo → calls domain method (`study.Activate(sig)`) → repo `Update` → `SaveChangesAsync`.
5. `AuditingSaveChangesInterceptor` (Infrastructure) fills `Updated*` + emits row-level `AuditEvent` records *in the same tx* — atomic.
6. `SavedChangesAsync` interceptor dispatches `DomainEvents` collected on the aggregate (Sprint 4+).

## Auth boundary

- **Web**: ASP.NET Core Identity + cookie auth. Issues JWT for downstream API calls (same process; cookies + JWT both flow).
- **Api**: JWT bearer (user) **or** `X-Api-Key` header (service-to-service). Authorization layered: scheme → policy (role + scope claim) → resource (per-aggregate via `IAuthorizationHandler`).

## Notes

- **Sprint 1 scaffold currently composes Web with direct Domain/Application/Infrastructure references** — see `03-module-dependencies.md` for the drift vs spec §7.2 ("Web → API over HTTP"). Pragmatic for Auto interactivity in v1; deferred clean-up.
- **No background workers as separate containers in v1** — `IHostedService` runs inside the API process (audit archival, signature TTL sweep, outbox flush). Worker Service split is a v2 seam.
- **No outbox dispatcher container** — outbox table schema is in place (Sprint 7+) but events dispatch in-process. v2 splits to a separate worker.
