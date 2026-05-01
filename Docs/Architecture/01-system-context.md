# 01 — System Context (C4 Level 1)

Highest-level view: who uses PharmaFlow and what external systems it talks to. Source: spec §2 (Roles), §3 (Entities), §8 (Stack).

```mermaid
flowchart TB
    classDef person fill:#08427b,stroke:#073b6f,color:#fff
    classDef system fill:#1168bd,stroke:#0b4884,color:#fff,stroke-width:2px
    classDef external fill:#999,stroke:#6b6b6b,color:#fff
    classDef stretch fill:#bbb,stroke:#888,color:#222,stroke-dasharray:5 3

    Sponsor["👤 Sponsor<br/>signs activation, suspends, closes"]:::person
    PI["👤 Principal Investigator<br/>signs site activation, ICF reviews"]:::person
    Coord["👤 Study Coordinator<br/>enrols subjects, captures consent"]:::person
    Auditor["👤 Auditor<br/>read-only inspection of audit trail"]:::person
    SysAdmin["👤 System Administrator<br/>user/role provisioning, signed"]:::person
    CRA["👤 CRA / Monitor<br/>(Stretch v1.5)"]:::stretch

    PharmaFlow["📦 PharmaFlow<br/>Clinical-trial study tracker.<br/>Studies, Sites, Subjects,<br/>Documents, eSignatures, Audit."]:::system

    Postgres[("🗄 PostgreSQL<br/>Flexible Server B1ms<br/>OLTP + audit table")]:::external
    Blob[("🗂 Azure Blob Storage<br/>WORM container for<br/>documents + audit cold archive<br/>(Sprint 7+)")]:::external
    KV[("🔐 Azure Key Vault<br/>signing keys + secrets<br/>(Sprint 8+)")]:::external
    SMTP[("📧 SMTP relay<br/>notifications<br/>(deferred)")]:::external
    Azurite[("🧪 Azurite (local only)<br/>Blob emulator")]:::external

    Sponsor --> PharmaFlow
    PI --> PharmaFlow
    Coord --> PharmaFlow
    Auditor --> PharmaFlow
    SysAdmin --> PharmaFlow
    CRA -.-> PharmaFlow

    PharmaFlow -->|"reads / writes<br/>(EF Core + Npgsql)"| Postgres
    PharmaFlow -->|"upload / download<br/>SAS URLs"| Blob
    PharmaFlow -->|"sign / verify<br/>(envelope encryption)"| KV
    PharmaFlow -.->|"future"| SMTP
    PharmaFlow -.->|"dev only"| Azurite
```

## Notes

- **All actors are humans inside the sponsor's org** — there is no patient-facing interface and no public API in v1. Subjects do not log in (per spec §2.7).
- **No third-party integrations in v1** — no EDC, no IRB system, no ERP. The system is intentionally a closed island so the demo never depends on external services being up.
- **Postgres is the system of record for audit** — append-only `AuditEvent` table with rule/trigger blocking UPDATE/DELETE (spec §10.5). Blob holds documents + an *archive* of older audit rows; Postgres remains canonical.
- **Key Vault is deferred to Sprint 8** — Sprint 2–7 use a `IKeyProvider` seam with an in-process implementation; KV adapter slots in without code changes elsewhere.
- **CRA / Monitor role is dashed** — v1.5 stretch goal, no Sprint 1–11 commitment.
