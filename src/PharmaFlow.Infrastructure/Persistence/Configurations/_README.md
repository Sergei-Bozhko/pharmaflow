# EF Core Entity Configurations — Index Plan

One `IEntityTypeConfiguration<T>` file per aggregate. `ApplyConfigurationsFromAssembly` in `AppDbContext.OnModelCreating` discovers all of them at model build.

Snake-case naming applied globally via `EFCore.NamingConventions` registered in `AppDbContextDesignTimeFactory` (Sprint 4 `Program.cs` mirrors). Property names below use the post-convention column form.

Partial indexes on `is_deleted` align with the soft-delete global query filter wired by **PFL-028**. `RowVersion` deferred to PFL-028 (xmin concurrency token) on every `Entity<TId>`-derived aggregate.

---

## `studies` (`StudyConfiguration`)

| Index | Type | Reason |
|---|---|---|
| `protocol_number` | unique | Protocol number is a business identifier. Duplicate ingest must fail at the DB level, not just domain validation. |
| `status` | non-unique | Sprint 4 list/filter queries (e.g. "active studies", "drafts pending approval"). |
| `is_deleted WHERE is_deleted = false` | partial | Matches global query filter from PFL-028; keeps the index small. |

## `sites` (`SiteConfiguration`)

| Index | Type | Reason |
|---|---|---|
| `study_id` | non-unique | FK lookup. Every Site list-by-Study query traverses this. |
| `principal_investigator_user_id` | non-unique | Reverse lookup ("which sites does this PI run?"). |
| `(study_id, site_number)` | unique | Site numbers must be unique within a study (protocol-level rule); not globally. |
| `status` | non-unique | Filter by site lifecycle state. |
| `is_deleted WHERE is_deleted = false` | partial | Soft-delete filter. |

## `participants` (`ParticipantConfiguration`)

| Index | Type | Reason |
|---|---|---|
| `site_id` | non-unique | FK lookup. Site enrolment lists. |
| `(site_id, subject_number)` | unique | Subject numbers unique within a site. |
| `enrolment_status` | non-unique | Sprint 4 status-faceted queries (screening / enrolled / withdrawn). |
| `is_deleted WHERE is_deleted = false` | partial | Soft-delete filter. |

## `users` (`UserConfiguration`)

| Index | Type | Reason |
|---|---|---|
| `username` | unique | Login lookup; uniqueness is a business invariant. |
| `email` | unique | Used for password-reset and identity proofing. |
| `status` | non-unique | Active vs disabled vs locked. |
| `is_deleted WHERE is_deleted = false` | partial | Soft-delete filter. |

## `role_assignments` (`RoleAssignmentConfiguration`)

`Scope` value object owned via `OwnsOne` → produces three columns on this table: `scope_kind`, `scope_study_id`, `scope_site_id`.

| Index | Type | Reason |
|---|---|---|
| `user_id` | non-unique | "What roles does this user have?" — most common access pattern. |
| `role` | non-unique | "Who are the PIs?" cross-cutting query. |
| `ended_at` | non-unique | Active-vs-ended filtering (`WHERE ended_at IS NULL`). |
| `is_deleted WHERE is_deleted = false` | partial | Soft-delete filter. |

`scope_kind` index deferred to PFL-031 — wait until concrete query patterns surface.

## `audit_events` (`AuditEventConfiguration`)

Append-only. **No** soft-delete, **no** row-version, **no** audit columns (Created/Updated). `Id` is `bigint` (long-backed `AuditEventId`).

| Index | Type | Reason |
|---|---|---|
| `occurred_at` | non-unique | Time-window queries (most common audit access pattern). |
| `actor_user_id` | non-unique | "What did this user do?" |
| `(target_entity_type, target_entity_id)` | non-unique | "Show me the history of this Study/Site/Participant." |
| `event_type` | non-unique | Faceted queries (e.g. all `StudyActivated` events). |
| `previous_event_hash` | non-unique | Hash-chain integrity walk; used by audit-log verification job. |

`before_state_json` and `after_state_json` stored as `jsonb` (Postgres native) — diffable, queryable via `->>` operator.

## `signature_records` (`SignatureRecordConfiguration`)

Append-only. Same shape as `audit_events`: no soft-delete, no row-version, no audit columns.

| Index | Type | Reason |
|---|---|---|
| `signer_user_id` | non-unique | "All signatures by this user." |
| `(target_entity_type, target_entity_id)` | non-unique | "Signature history for this entity." |
| `signed_at` | non-unique | Time-range reports. |
| `meaning` | non-unique | Faceted queries (e.g. all `Approval` signatures). |
| `previous_signature_hash` | non-unique | Hash-chain integrity walk. |

---

## Conventions applied

- **Enums**: stored as `varchar` via `HasConversion<string>()`. Audit before/after JSON shows `"Status": "Active"` not `"Status": 3`. Diff readability over byte savings.
- **Hashes**: 64-char hex, `HasMaxLength(64).IsFixedLength()` — Postgres `char(64)`.
- **IPs**: `HasMaxLength(45)` — IPv6 address max length.
- **Country codes**: `HasMaxLength(2).IsFixedLength()` — ISO 3166-1 alpha-2.

## Out of scope (this ticket)

- Explicit `HasOne / WithMany` FK relationships — typed-ID convention from PFL-026 produces shadow FKs that work for now. Revisit when Sprint 4+ access patterns demand explicit nav properties.
- Migration generation — PFL-029.
- `OwnsOne` for `SignatureMeta` — Sprint 2 domain holds it as method parameters only, not stored fields.
