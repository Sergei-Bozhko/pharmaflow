# PharmaFlow — Technical Specification (v1)

> Living portfolio/learning project. A clinical-trial / study-tracker that demonstrates GxP-aligned regulated-software engineering on a modern .NET 10 + Azure stack. This document is the single source of truth for scope, architecture, security, compliance, and delivery sequencing for v1. It is intentionally detailed so it can be used as both an implementation playbook and a CV/interview talking-point reference.

---

## Context

**Why this project exists.** The owner is a returning enterprise C# developer (last touched .NET ~C# 6 / WebForms / old MVC / ADO.NET) who is rebuilding a credible public engineering profile in order to land a remote Senior .NET role with a regulated-industry / pharma-tech focus by 2027. PharmaFlow is **Project 1** of three (PharmaFlow → DocChat → ReqGather) and is the anchor portfolio piece scheduled across Months 2–4 of the 6-month learning plan (`LearningPlan.md`). It is *not* a real production product; it is a deliberately-scoped portfolio simulation that proves competence in:

- Modern .NET 10 + EF Core + Clean Architecture + CQRS
- Azure deployment (App Service, SQL/Postgres, Key Vault, Blob, App Insights)
- Regulated-software engineering: 21 CFR Part 11, ALCOA+, GAMP 5 vocabulary, controlled-document workflow, electronic signatures, immutable audit trail
- Pharma domain literacy (Sponsor / Investigator / Coordinator / Auditor; Study / Site / Subject / ICF / SOP)

**Primary outcomes when shipped:**
1. A live demo at `https://pharmaflow-<owner>.azurewebsites.net` with seeded synthetic data.
2. A public GitHub repo with README, architecture diagram, ADRs, screenshots, Loom walkthrough.
3. 7+ "anchor sentences" usable verbatim in CV bullets and interview answers (see §27).

**Repo location:** `/Users/sergeybozhko/Coding/C-sharp/learning/sandbox/PharmaFlow/` (follows existing `/sandbox/<project>/` convention used by `HelloDotNet8` and `ExploreMiddleware`).

**Time budget:** ~10 hrs/week × ~12 weeks = ~120 hours. Scope-cuts are non-negotiable; see §10.

---

## 1. Domain Primer (concise)

### 1.1 What a clinical study/trial actually is

A controlled investigation in human subjects to evaluate the safety, efficacy, pharmacokinetics, or other properties of a medicinal product, device, or intervention. Run by a **Sponsor** (typically a pharmaceutical company that owns the molecule and is legally accountable), often delegated operationally to a **CRO** (Contract Research Organisation). Conducted at one or more **Sites** (hospitals, clinics) under a **Principal Investigator (PI)** who is the licensed clinician legally responsible for the conduct of the trial at that site. Subjects are recruited, must provide **Informed Consent** before any trial-specific procedure, and are then followed per a fixed schedule of visits.

### 1.2 Lifecycle (the four phases that matter for tooling)

- **Study Setup** — protocol authoring/approval, regulatory & ethics submission (FDA IND / EMA CTA / IRB / EC), site selection & qualification, system go-live, investigator training, SOP roll-out.
- **Recruitment / Enrolment** — sites identify eligible patients, obtain Informed Consent, screen against inclusion/exclusion criteria, and randomise/enrol. Consent must precede any study procedure.
- **Conduct** — subjects come in for scheduled visits, data captured on **CRFs**, adverse events recorded, monitors review source data vs CRF, deviations logged, Sponsor's medical/safety team reviews SAEs.
- **Closeout** — last subject last visit (LSLV), database lock, final monitoring visit, statistical analysis, Clinical Study Report (CSR), archival (records retained 15–25 years per jurisdiction).

### 1.3 Key artifacts the system cares about

| Artifact | Meaning |
|---|---|
| **Protocol** | Master document defining objectives, design, eligibility, visit schedule, endpoints, statistics. Versioned; amendments require re-approval. |
| **ICF** (Informed Consent Form) | Site- and language-specific consent document. Subject signs the *current* IRB-approved version, never an older one. |
| **CRF** (Case Report Form) | Structured data capture instrument per visit (vitals, labs, AE log…). In real life lives in an **EDC** system. *PharmaFlow does NOT implement EDC capture.* |
| **SOP** (Standard Operating Procedure) | Controlled internal process documents. Sign-off and effective-date discipline are core. |
| **Audit Trail** | Immutable record of who did what, when, why, to which record. |
| **eSignature record** | Cryptographically/structurally bound assertion of a user's intent (approve, reject, witness) against a specific record version. |

### 1.4 GxP / 21 CFR Part 11 / ALCOA+ in plain terms

- **GxP** — umbrella for "Good *x* Practice" (GCP=Clinical, GMP=Manufacturing, GLP=Lab, GDP=Distribution). Clinical trials run under **GCP** (ICH E6).
- **21 CFR Part 11** — US FDA regulation governing electronic records & signatures. EU equivalent: **EU Annex 11**. Plain-English requirements:
  - Records must be attributable to a uniquely-identified, authenticated user.
  - System must produce accurate, complete copies (human-readable + electronic).
  - Records must be protected and accurately retrievable for the retention period.
  - Access must be limited to authorised individuals (RBAC).
  - There must be a **secure, computer-generated, time-stamped audit trail** that records all create/modify/delete actions on electronic records and does not obscure prior information.
  - Electronic signatures must include the printed name of the signer, date+time, and meaning ("approved", "reviewed").
  - Electronic signatures must be linked to their records so they cannot be excised, copied, or transferred.
- **GAMP 5** — ISPE risk-based validation framework for computerised systems. Categorises software (Cat 1 infra → Cat 5 custom). PharmaFlow is Cat 5. We will not *do* validation, but produce light artefacts (URS, traceability matrix, IQ/OQ/PQ-style test evidence) that look like they belong.
- **ALCOA+** — data-integrity mnemonic regulators apply to every record:
  - **A**ttributable — you can tell who created/changed it
  - **L**egible — readable, including by future readers
  - **C**ontemporaneous — recorded at the time of the event
  - **O**riginal — first capture preserved (or a verified true copy)
  - **A**ccurate — correct, with corrections traceable
  - **+** **C**omplete, **C**onsistent, **E**nduring, **A**vailable

These principles drive the compliance behaviours in §6.

---

## 2. User Roles & Responsibilities

Five MVP roles. **Subjects are NOT system users** in MVP — coordinators capture consent on a shared device.

### 2.1 Sponsor (MVP)
- **Who.** Pharma company representative who owns the trial — typically a Clinical Project Manager. Portfolio-wide visibility.
- **Does.** Creates Study, uploads Protocol, defines sites, assigns PIs, approves SOPs, reviews aggregated enrolment status, reviews study-level audit trail, signs study-level milestones (activation, suspension, closure, database lock).
- **Reads.** Everything in their studies (study metadata, enrolment counts, document statuses, audit events, deviations). No raw subject PII.
- **Writes.** Study setup data, protocol/SOP uploads, site assignments, study-level signatures.

### 2.2 Principal Investigator (MVP)
- **Who.** Licensed clinician at a site, legally responsible for trial conduct. In real life signs FDA Form 1572.
- **Does.** Reviews and signs Protocol and ICF for their site, signs off on subject enrolment decisions, signs off on SAE reports, reviews their site's audit trail and deviations.
- **Reads.** Their own site's data only. Cannot see other sites.
- **Writes.** Investigator signatures on documents, sign-off on subject eligibility/enrolment, deviation reports for their site.

### 2.3 Study Coordinator (MVP)
- **Who.** Site-level operations person (often a research nurse). Highest-volume user in any real CTMS.
- **Does.** Registers prospective subjects (screening), captures Informed Consent (presents current ICF version, captures subject signature, captures own witness signature), schedules visits, records protocol deviations, uploads source documents.
- **Reads.** Their site's subjects, current ICF and protocol versions, SOPs, visit schedule.
- **Writes.** Subject records, consent records, deviation records, witness signatures.
- *Justification:* without this role the consent workflow has no driver and RBAC story collapses to "Sponsor edits everything".

### 2.4 Auditor (MVP)
- **Who.** Internal QA, external auditor, or regulatory inspector (FDA, MHRA, EMA).
- **Does.** Reads audit trail, exports audit reports, verifies eSignatures, verifies document version history, reviews deviations. **Strictly read-only** — auditors who can mutate are a regulatory finding.
- **Reads.** Everything in scope (study- or system-wide), with special privilege to read the audit log itself.
- **Writes.** Nothing.

### 2.5 System Administrator (MVP)
- **Who.** IT/QA hybrid. Manages users, roles, password resets, system config.
- **Does.** Provisions/deactivates users, assigns roles, manages reference data. **Cannot view trial data; cannot approve trial documents.** Segregation-of-duties is the headline.
- **Reads.** User accounts, role assignments, system config, system-level audit events (logins, lockouts).
- **Writes.** User accounts and role assignments only.

### 2.6 CRA / Monitor (Stretch — v1.5)
Sponsor- or CRO-employed monitor who verifies CRF data against source documents (Source Data Verification, SDV). Introduces a query/resolution workflow. Skip for v1.

### 2.7 Participant / Subject (NOT a system user)
Subject signs consent on a device handed to them by the Coordinator. Data subject, not application user. No login, no portal. Realistic and avoids GDPR/HIPAA scope explosion.

---

## 3. Core Entities (Domain Model)

All entities use **strongly-typed IDs** (`StudyId(Guid)`, `ParticipantId(Guid)`, etc.). All entities except `AuditEvent` and `SignatureRecord` carry a base `Entity<TId>` with: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `RowVersion` (concurrency token), `IsDeleted` (soft delete only). AuditEvent and SignatureRecord are append-only and intentionally omit `Update*` and `IsDeleted`.

### 3.1 Study
- **Purpose.** Top-level trial record.
- **Attributes.** StudyId, ProtocolNumber (e.g. `PFL-2025-001`), Title, Phase (I/II/III/IV), TherapeuticArea, SponsorOrganisation, PlannedEnrolment, PlannedStartDate, PlannedEndDate, Status.
- **Relationships.** Has many Sites, Documents (Protocol + amendments), Participants (via Sites), AuditEvents.
- **Lifecycle.** `Draft` → `PendingApproval` → `Active` → `Suspended` → `Closed` → `Archived`. Activation/suspension/closure all require Sponsor signature.

### 3.2 Site
- **Purpose.** Physical location where the study is conducted.
- **Attributes.** SiteId, StudyId, SiteNumber, Name, Country, PrincipalInvestigatorUserId, ActivationDate, Status.
- **Lifecycle.** `Selected` → `Qualified` → `Initiated` → `Active` → `Closed`. Activation requires Sponsor + Investigator signatures.

### 3.3 Investigator (modelled as a User role + SiteUserAssignment)
Not a top-level entity. `User` + `SiteUserAssignment` (SiteUserAssignmentId, SiteId, UserId, Role=PI/Sub-I/Coordinator, AssignedAt, EndedAt). Avoids dual-source-of-truth.

### 3.4 Participant (Subject)
- **Purpose.** Trial subject. **Pseudonymised** — system stores SubjectNumber (`S-001-024`), never real name/address/SSN.
- **Attributes.** ParticipantId, SiteId, SubjectNumber, EnrolmentStatus, ScreeningDate, EnrolmentDate, WithdrawalDate, WithdrawalReason, Initials (optional, encrypted), YearOfBirth (not full DOB), Sex.
- **Lifecycle.** `Prospective` → `Screening` → `ScreenFailed` | `Consented` → `Enrolled` → `Active` → `Completed` | `Withdrawn` | `LostToFollowUp`. Each transition records reason.

### 3.5 Document
- **Purpose.** Any controlled document — Protocol, ICF, SOP, Investigator Brochure, training material.
- **Attributes.** DocumentId, DocumentType (enum: Protocol | ProtocolAmendment | ICF | SOP | InvestigatorBrochure | Other), Title, OwningStudyId (nullable — system-wide SOPs), CurrentVersionId.
- **Lifecycle.** `Draft` → `InReview` → `Approved` → `Effective` → `Superseded` | `Retired`.

### 3.6 DocumentVersion
- **Purpose.** Immutable specific version. Once `Effective`, **cannot be edited** — a new version must be created.
- **Attributes.** DocumentVersionId, DocumentId, VersionNumber (`1.0`, `1.1`, `2.0`), FileBlobReference, Checksum (SHA-256), EffectiveDate, SupersededDate, SupersededByVersionId, ReasonForChange.
- **Lifecycle.** `Draft` → `PendingSignature` → `Effective` → `Superseded` | `Retired`. Promotion requires the prescribed signature set per DocumentType.

### 3.7 ConsentRecord
- **Purpose.** Captures that a subject consented to a specific ICF version on a specific date with subject + coordinator (witness) + investigator (confirmation) signatures.
- **Attributes.** ConsentRecordId, ParticipantId, IcfDocumentVersionId, ConsentedAt, SubjectSignatureBlobReference, CoordinatorSignatureId, InvestigatorSignatureId, WithdrawalDate, WithdrawalReason.
- **Lifecycle.** `Pending` → `Active` → `Withdrawn` | `Reconsented`.
- **Critical rule.** `IcfDocumentVersionId` must reference a version whose status was `Effective` *at the time of consent*. Audit log proves this forever — high-value compliance demonstration.

### 3.8 SignatureRecord (eSignature)
- **Purpose.** Cryptographic/structural assertion that a User performed an intent against a record at a time.
- **Attributes.** SignatureId, SignerUserId, SignedAt (UTC), SignatureMeaning (enum: Approved | Reviewed | Witnessed | Rejected | Authored), TargetEntityType, TargetEntityId, TargetVersionOrHash, ReasonStatement (mandatory), AuthenticationMethod (PasswordReentry | TOTP), SignaturePayloadHash, PreviousSignatureHash (chain), ClientIp, UserAgent, MfaMethod, ContinuousSession (bit), CorrelationId, SigningKeyId (KV key version).
- **Lifecycle.** Immutable. No update. No delete.
- **Critical rule.** Bound to content snapshot via TargetVersionOrHash. If the underlying record is later versioned, prior signatures stay attached to the prior version (Part 11 §11.70).

### 3.9 AuditEvent
- **Purpose.** Immutable audit trail. Every Create/Update/SoftDelete/sensitive Read emits an event.
- **Attributes.** Id (bigint identity), OccurredAt (UTC), ActorUserId, ActorRoleAtTime, EventType (Create | Update | SoftDelete | Read | Login | LoginFailed | RoleChange | SignatureApplied | DocumentEffective | ConsentCaptured | StatusTransition | KeyRotation), TargetEntityType, TargetEntityId, BeforeStateJson, AfterStateJson, ReasonForChange, SourceIpAddress, ClientInfo, EventPayloadHash, PreviousEventHash (chain).
- **Lifecycle.** Append-only. No update. No delete. Ever.

### 3.10 User
- **Purpose.** Authenticated principal.
- **Attributes.** UserId, Username, Email, FullName (printed-name for signature), DisplayTitle (e.g. "MD"), Status (Active | Locked | Deactivated), MfaEnrolled, LastLoginAt, FailedLoginCount, PasswordLastChangedAt.
- **Lifecycle.** `Invited` → `Active` → `Locked` → `Deactivated`. Deactivation soft; user history must remain attributable forever.

### 3.11 RoleAssignment
- **Purpose.** Binds a User to a Role within a scope.
- **Attributes.** RoleAssignmentId, UserId, Role (Sponsor | Investigator | Coordinator | Auditor | SystemAdmin), Scope (System | Study:{StudyId} | Site:{SiteId}), AssignedAt, EndedAt, AssignedBySignatureId.
- **Critical rule.** Role-assignment changes are themselves signed events (Sys Admin signs).

### 3.12 ProtocolDeviation (Stretch — v1.5)
DeviationId, ParticipantId, DeviationDate, Category, Severity (Minor | Major | Critical), Description, RootCause, CorrectiveAction, ReportedBySignatureId, AcknowledgedBySignatureId. Lifecycle: `Reported` → `UnderReview` → `Acknowledged` → `Closed`.

---

## 4. Use Cases / User Stories

`[MVP]` = build for v1; `[Stretch]` = nice-to-have; `[v2]` = explicitly cut.

### 4.1 Study Setup
- **US-01 [MVP]** As a Sponsor, I want to create a new Study with protocol number, title, phase, and planned enrolment.
- **US-02 [MVP]** As a Sponsor, I want to upload a Protocol document (PDF) and assign it to the study.
- **US-03 [MVP]** As a Sponsor, I want to define sites and assign a PI to each.
- **US-04 [MVP]** As a Sponsor, I want to transition a Study from Draft to Active by applying my eSignature with meaning "Approve study activation".
- **US-05 [Stretch]** As a Sponsor, I want to issue a Protocol Amendment and require re-signature by all PIs.

### 4.2 Document Management & Sign-Off
- **US-06 [MVP]** As a Sponsor, I want to upload a new version of a document with a mandatory Reason for Change.
- **US-07 [MVP]** As an Investigator, I want to review and electronically sign the Protocol and ICF for my site.
- **US-08 [MVP]** As any user, I want to see version history with effective dates and supersession chain — to answer "what was effective on date X".
- **US-09 [MVP]** As a Sponsor, I want a document to become Effective only when all required signatures have been captured.

### 4.3 Participant Management & Consent
- **US-10 [MVP]** As a Coordinator, I want to register a prospective subject with a generated SubjectNumber (no PII).
- **US-11 [MVP]** As a Coordinator, I want to capture Informed Consent against the *currently Effective* ICF version, with subject + my witness + Investigator confirmation signatures.
- **US-12 [MVP]** As a Coordinator, I want to transition a subject through screening → enrolled → completed/withdrawn states with reason capture on every transition.
- **US-13 [Stretch]** As a Coordinator, when a Protocol Amendment is issued, I want active subjects flagged as requiring re-consent.

### 4.4 Audit Log & Compliance Viewing
- **US-14 [MVP]** As an Auditor, I want to view audit trail for any record showing who/what/when/why and before/after state.
- **US-15 [MVP]** As an Auditor, I want to export audit trail to PDF/CSV with hash-chain verification result.
- **US-16 [MVP]** As an Auditor, I want to verify a given eSignature is intact and bound to the exact record version it signed.
- **US-17 [Stretch]** As an Auditor, I want to filter the audit trail by user, date range, and event type.

### 4.5 Role-Based Dashboards
- **US-18 [MVP]** Sponsor dashboard: enrolment progress, document sign-off completeness, outstanding signatures.
- **US-19 [MVP]** Investigator dashboard: site subjects, pending signatures, outstanding deviations.
- **US-20 [MVP]** Coordinator worklist: consents to capture, subjects to update, training acknowledgements due.
- **US-21 [MVP]** Auditor landing: recent audit events with hash-chain status across assigned scope.

### 4.6 User & Access Administration
- **US-22 [MVP]** As a Sys Admin, I want to invite a user, assign roles scoped to studies/sites, with all role changes audit-logged.
- **US-23 [MVP]** As any user, I want to authenticate with username + password + TOTP.
- **US-24 [MVP]** As any user applying an eSignature, I want to be required to re-enter a credential with reason and signature meaning (Part 11 §11.200).

**MVP scope ≈ stories 01–04, 06–12, 14–16, 18–24 (≈19 stories). Stretch: 05, 13, 17 + ProtocolDeviation + CRA queries.**

---

## 5. Compliance Behaviours That MUST Exist

These are the differentiators between "CRUD app with a roles dropdown" and "regulated software". Each maps to a regulatory clause and produces a CV bullet.

### 5.1 Immutable Audit Trail (Part 11 §11.10(e))
Every Create / Update / SoftDelete / sensitive Read produces an `AuditEvent` row. Captures actor, role-at-time, UTC timestamp, event type, target entity + ID, before/after JSON snapshot, reason, source IP, client info.

**Append-only enforcement:**
- App SQL principal `app_writer` has `SELECT, INSERT` only on `AuditEvents`. **Explicitly DENY UPDATE, DELETE.**
- A separate `app_admin` principal exists for migrations / emergency repair. Stored in Key Vault, manual gate, alert on use.

**Hash chain.** Each event stores `EventPayloadHash = SHA-256(canonicalised event)` and `PreviousEventHash` linking to the previous event. Insertion serialised via app-level lock (`sp_getapplock` SQL Server, advisory lock Postgres). A verifier endpoint replays the chain to detect tampering. *This is the feature that screams "I know what tamper-evidence means".*

### 5.2 Electronic Signatures (Part 11 Subpart C)
- Re-authenticate at the moment of signing (Part 11 §11.200(a)(1) "two distinct identification components").
- Capture **printed name**, **date+time** (UTC + display TZ), **meaning** (Approve / Review / Witness / Reject / Author) — §11.50.
- **Bound to record content** via `SignaturePayloadHash` including target entity's content hash or version ID. If record is versioned, signature stays on the prior version — cannot transfer.
- **Non-repudiable**: cannot be excised, copied, transferred. Soft-delete forbidden — persist with deactivated user account.
- **Mandatory free-text reason** on every signature.
- **Manifestation** rendered in human-readable form alongside any signed record (§11.50(b)).

### 5.3 Document Versioning & Sign-Off Workflow
- Documents immutable once `Effective`. Edits create a new `DocumentVersion` with mandatory Reason for Change.
- Each DocumentType has a configurable **required signature set** (e.g. Protocol = Sponsor + per-site PI; SOP = Sponsor + QA review).
- Version transitions to `Effective` only when all required signatures captured; EffectiveDate recorded.
- Prior version marked `Superseded` with `SupersededByVersionId` and `SupersededDate`. Never deleted.
- Effective-date queries: "what version of ICF-EN-v1 was Effective on 2026-03-14?" must return a deterministic answer.

### 5.4 Reason-for-Change Capture
- Every mutation to a non-draft record requires a free-text reason (min 5 chars, max 512). Drafts pre-effective don't.
- Reason stored on AuditEvent and, where relevant, on the entity (DocumentVersion, subject status transitions).
- Enforced at API boundary (FluentValidation → 400) and at audit interceptor (defence-in-depth).

### 5.5 Soft Delete Only
- No hard deletes anywhere except: (a) cancelled drafts pre-first-signature, (b) GDPR right-to-erasure (out of scope — no real PII).
- Every entity has `IsDeleted` + `DeletedAt` + `DeletedBy` + `DeletionReason`.
- Listing/detail queries filter `IsDeleted` by default; auditors get an "include deleted" toggle.

### 5.6 ALCOA+ Manifestations
- **Attributable** — CreatedBy + LastModifiedBy + role-at-time on audit events; signatures carry signer identity.
- **Legible** — UTF-8 storage; human-readable rendering; rendered preview for binary blobs.
- **Contemporaneous** — server-side UTC timestamping; client timestamps recorded but not authoritative.
- **Original** — first-capture preserved; corrections create new versions/audit events, never silently overwrite.
- **Accurate** — validation rules at capture; reason-for-change on edit.
- **Complete** — soft-delete preservation; full audit trail; no orphan signatures.
- **Consistent** — single time source (server UTC), strongly-typed enums, deterministic state transitions.
- **Enduring** — append-only audit + hash chain; document versions retained; signatures retained.
- **Available** — read endpoints accessible to authorised roles; export to PDF/CSV for inspection.

### 5.7 Authentication & Session Controls
- Password complexity per NIST 800-63B (length 12+, no forced rotation).
- Account lockout after 5 failed attempts, audit-logged.
- Session timeout with explicit re-auth required before any signature event (step-up).
- TOTP MFA required for Sponsor, Investigator, Coordinator, Sys Admin, Auditor.

### 5.8 Segregation of Duties
- Sys Admin cannot view trial data and cannot apply trial-business signatures.
- Auditor cannot mutate trial data.
- Author of a document cannot be the *sole* approver (configurable rule; soft warning in v1, document the principle).
- Same user cannot hold Investigator + Auditor on the same study.

---

## 6. Out of Scope for v1

This is the most important section. Cut aggressively and tell the *why* in the README — that itself is a hiring signal.

### Explicitly NOT building
- **Real EDC** (CRF design, visit data capture, edit checks). Veeva Vault EDC and Medidata Rave are mature verticals; rebuilding eats months.
- **Real PII / PHI handling** — no patient names, addresses, full DOBs, identifiers. Subject pseudonyms only. Dodges GDPR/HIPAA/PHI scope.
- **HL7 / FHIR / EHR integration.**
- **Lab integrations / LIMS.**
- **IRT / RTSM** (Randomisation and Trial Supply Management).
- **Safety / Pharmacovigilance reporting** (E2B, MedWatch, EudraVigilance). SAEs as deviations only, if at all.
- **Multi-tenant SaaS / billing / org hierarchies.** Single-tenant.
- **Subject-facing portal / eConsent app.** Coordinator drives consent capture.
- **Real regulatory submission** (eCTD, ESG).
- **Full validation package** (URS/FS/IQ/OQ/PQ as deliverable). Produce *one* representative trace-matrix entry.
- **GDPR right-to-erasure flow** — moot, no real PII.
- **Legal-grade digital signatures** (PKI / qualified certs per eIDAS). Part 11 does *not* require PKI.
- **Native mobile apps.** Web-responsive only.
- **Real-time collaborative document editing.** Upload-only.
- **Workflow engine / BPMN.** Hard-code small state machines; no Camunda/Elsa.
- **Internationalisation.** English only; UTC + one display TZ.
- **Microservices / event sourcing / Kubernetes / Service Bus / Kafka / GraphQL / gRPC.**
- **Custom OAuth provider (Entra External ID, Auth0, Okta).** ASP.NET Core Identity for v1; document the swap path.
- **Distributed cache (Redis).**
- **Full SOC 2 / ISO 27001 / HIPAA / HITRUST audit.**
- **Private Endpoints / VNet / WAF / Front Door.**
- **Real AV scanning of uploads.** Quarantine pattern as placeholder.
- **Multi-region failover.**
- **Customer-managed keys (CMK) / HSM-backed Key Vault Premium.**
- **AutoMapper.** Mapperly only.
- **Bicep/Terraform IaC.** `az` CLI scripts in `/infra/scripts` for v1; IaC is a Month-5+ stretch.
- **Azure DevOps Boards / Pipelines.** GitHub Actions only.

### Stretch (only if v1 on rails by month 2)
- ProtocolDeviation module
- Re-consent flow on protocol amendment
- CRA / Monitor query workflow
- Same-user-cannot-author-and-approve hard rule
- Audit trail filter UI
- Document training acknowledgement
- HIBP breached-password check
- Bicep IaC
- Entra External ID alternate auth path

---

## 7. Solution Architecture & Layout

### 7.1 Project structure

Seven projects under `/sandbox/PharmaFlow/`. `src/` and `tests/` split is the modern .NET template convention.

```
/sandbox/PharmaFlow/
├── PharmaFlow.sln
├── Directory.Build.props
├── Directory.Packages.props
├── .editorconfig
├── global.json
├── README.md
├── docs/
│   ├── adr/                       (Architecture Decision Records)
│   ├── domain-spec.md             (this file, copied/linked)
│   └── compliance/
│       ├── part11-mapping.md
│       └── alcoa-plus-mapping.md
├── infra/
│   └── scripts/                   (az CLI provisioning scripts, v1)
├── src/
│   ├── PharmaFlow.Domain/
│   ├── PharmaFlow.Application/
│   ├── PharmaFlow.Infrastructure/
│   ├── PharmaFlow.Api/
│   └── PharmaFlow.Web/             (Blazor Web App, Auto interactivity)
└── tests/
    ├── PharmaFlow.Tests.Unit/
    └── PharmaFlow.Tests.Integration/
```

### 7.2 Per-project responsibilities and dependency rules

| Project | Purpose | Depends on | MUST NOT depend on |
|---|---|---|---|
| **PharmaFlow.Domain** | Pure C#: entities, aggregates, value objects, strongly-typed IDs, domain events, domain exceptions, domain interfaces (`IClock`, `IDomainEventDispatcher`). No frameworks. | BCL only (+ `Result`/`Error` primitives). | EF Core, ASP.NET, MediatR, FluentValidation, Serilog, Mapperly. |
| **PharmaFlow.Application** | CQRS commands/queries + handlers, validators, pipeline behaviors, DTOs, application-layer ports (`IStudyRepository`, `ICurrentUser`, `IAuditWriter`, `IDocumentStorage`, `IAppDbContext`). | Domain, MediatR, FluentValidation, Mapperly attributes. | EF Core, ASP.NET hosting, Azure SDKs, Serilog (use `Microsoft.Extensions.Logging.Abstractions`). |
| **PharmaFlow.Infrastructure** | Concrete adapters: `AppDbContext`, EF Core configurations, migrations, repository impls, `SaveChangesInterceptor` for audit, Blob adapter, JWT issuer, email sender, clock, OTel exporters wiring. | Application, Domain, EF Core, Npgsql, Azure SDKs, Serilog. | ASP.NET hosting framework, Api, Web. |
| **PharmaFlow.Api** | Composition root for HTTP. Minimal API endpoint groups, ProblemDetails, OpenAPI, auth wiring, DI registration, `Program.cs`. | Application, Infrastructure, `Microsoft.AspNetCore.App`. | EF Core types directly in endpoints — go through MediatR. |
| **PharmaFlow.Web** | Blazor Web App (Auto, .NET 10). Talks to API over HTTP. | Application DTOs (or shared `Contracts` if introduced) for shared types. | EF Core, Infrastructure. |
| **PharmaFlow.Tests.Unit** | Domain + Application handler tests. Pure, fast, no I/O. | Domain, Application, xUnit, FluentAssertions, NSubstitute. | Infrastructure, real DB. |
| **PharmaFlow.Tests.Integration** | API-level tests via `WebApplicationFactory<Program>` against real Postgres in Testcontainers. | Api, Infrastructure, `Testcontainers.PostgreSql`, xUnit. | Mocking the DB. |

### 7.3 Solution file decisions
- Use classic `.sln` (not `.slnx`). Tooling support still patchy on macOS for `.slnx`. Migrate later via `dotnet sln migrate`.
- **Do NOT add PharmaFlow projects to the root `learning.sln`.** Keep PharmaFlow self-contained — own solution, own build target. Avoids loading 7 projects into Rider/VS for unrelated sandbox work.

### 7.4 `global.json`

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

---

## 8. Stack Picks (opinionated)

| Concern | Pick | Rationale |
|---|---|---|
| .NET version | **net10.0** all projects | Aligns with curriculum; Minimal API improvements; built-in OpenAPI document generation. |
| Database | **PostgreSQL** (Azure DB for PostgreSQL Flexible Server B1ms) | Cheaper at low end on Azure; Testcontainers rock-solid; `docker run postgres` matches prod; native JSONB; Apple-Silicon-friendly local dev. Trade-off: lose Azure SQL temporal tables — but we want to *build* the audit pipeline ourselves. |
| ORM | **EF Core 10** | Scoped DbContext per request; owned types; value converters for typed IDs; global query filters for soft-delete + tenancy seam; no lazy loading; `AsNoTracking()` on reads; compiled queries only after profiling. |
| API style | **Minimal APIs, feature-grouped endpoints** | Curriculum-aligned; `MapGroup("/api/v1/studies")...` reads cleanly; endpoint filters give cross-cutting hooks. |
| Mediator | **MediatR 12.4.x** (Apache 2.0) | Free for v1; ecosystem-aligned; swap to `Mediator` (martinothamar) is a 2-hour mechanical refactor if licensing changes for v13+. |
| Result pattern | **Hand-rolled `Result`/`Result<T>` + `Error` record** | ~80 lines; learning value; pairs cleanly with ProblemDetails switch expression. In `Domain`. |
| Validation | **FluentValidation 11.x** | Each command/query gets a validator; hooked into MediatR via `ValidationBehavior`; fail fast → `Error.Validation`. |
| Mapping | **Mapperly** (source generator) | Compile-time; debugger-friendly; no runtime DI ceremony; no AutoMapper. |
| Identity | **ASP.NET Core Identity + custom JWT issuer** | Full control over hashing, lockout, MFA, refresh rotation — strong portfolio storytelling. Entra External ID as v1.1 stretch. |
| Logging | **Serilog** (Console JSON, App Insights via OTLP) | Structured; correlation IDs from W3C TraceContext; redaction enricher for `password`/`token`/`signatureValue`. |
| Observability | **OpenTelemetry → Azure Monitor / App Insights** | Instrumentation: AspNetCore, HttpClient, EF Core, Azure SDKs, Npgsql. Custom `Meter` + `ActivitySource`. |
| Frontend | **Blazor Web App, Auto interactivity** (.NET 10) | Stay in C#; one language one build; .NET 10's flagship UI story. Trade-off: less SPA/TS exposure — accept and document. |
| Background work | **`IHostedService` / `BackgroundService`** in API process | Sufficient for periodic audit archival, signature TTL sweep, outbox notification flush. No Quartz, no Functions. Worker Service split is a v2 seam. |
| Document storage | **Azure Blob Storage** | Versioning + immutability policies (WORM container for audit cold-archive); SAS URLs; Azurite locally. DB stores blob URI + SHA-256 + size + content-type + uploader + timestamps. |

---

## 9. Layered Patterns

### 9.1 Domain layer
- Aggregates with private setters. Mutate via methods. `Study.AddParticipant(...)`, `Study.SignOff(SignatureMeta sig)`.
- Value objects as `record` types (immutable, structural equality).
- Strongly-typed IDs: `readonly record struct StudyId(Guid Value)`. **No implicit conversions** — force callers explicit. `New()` (UUIDv7 via `Guid.CreateVersion7()`) and `Empty` static members.
- Domain events as records implementing `IDomainEvent`. Collected on a base `Entity.DomainEvents` list, dispatched **after** `SaveChanges` succeeds.
- Factory methods (`Study.Create(...)`) return `Result<Study>` — invariants enforced at construction. Constructors private/internal.
- Domain exceptions only for invariant violations that should never be reached. Most failures come back as `Result.Failure(error)`.

### 9.2 Application layer
- CQRS via MediatR `IRequest<Result<T>>`. Folder per feature.
- Commands mutate, return `Result` or `Result<TId>`. Queries read-only, return `Result<TDto>` or `Result<PagedList<TDto>>`.
- Handlers thin: pull aggregate via repo / `IAppDbContext` → call domain method → persist → map → return.
- **Pipeline behaviors** (outer → inner):
  1. `LoggingBehavior` — span + timing + outcome
  2. `ValidationBehavior` — FluentValidation, short-circuit on failure
  3. `IdempotencyBehavior` — checks `Idempotency-Key` for `IIdempotentCommand`
  4. `TransactionBehavior` — opens EF transaction for `ICommand`, commits/rolls back
  5. `AuditBehavior` — high-level audit event for command outcome (separate from row-level interceptor)
- Query handlers bypass `TransactionBehavior` (no marker). They use `AsNoTracking()` projections through `IAppDbContext` — not repositories. Repositories command-side only.
- DTOs live next to their handler in feature folder.

### 9.3 Infrastructure layer
- `AppDbContext` exposed as `IAppDbContext` (read-only `IQueryable<T>` + `SaveChangesAsync`) to Application for queries.
- Per-aggregate repositories on the command side. **No `IRepository<T>` generic.**
- `AuditingSaveChangesInterceptor` on `SavingChangesAsync`:
  - Fills `CreatedAt`/`UpdatedAt`/`CreatedBy`/`UpdatedBy` on `IAuditableEntity`.
  - For changed `IAuditedEntity` entries, emits `AuditEvent` rows with old/new JSON snapshots, entity name, key, change type, user, timestamp, correlation ID, hash chain.
  - Audit rows inserted in *same* transaction → atomic with business mutation.
- Domain event dispatcher runs *after* `SaveChangesAsync` succeeds (`SavedChangesAsync` interceptor or unit-of-work wrapper).
- **Outbox pattern** — seam exists (table schema documented), not implemented in v1. All v1 event handlers in-process.

### 9.4 API layer
- Minimal API endpoint groups per feature, URL versioned (`/api/v1/...`). Use `Asp.Versioning.Http` for v2 readiness.
- ProblemDetails (RFC 7807). `Error` → `ProblemDetails` mapping in `Api/Common/ResultExtensions.cs` via `result.ToHttpResult(httpContext)`.
- OpenAPI via `Microsoft.AspNetCore.OpenApi` (.NET 10 built-in). Pair with Scalar or NSwag UI for interactive docs.
- Auth: JWT bearer for users + API key (`X-Api-Key`) for service-to-service.
- Endpoint filters: rate-limit signaling, idempotency-key enforcement, audit-context capture (correlation ID).

### 9.5 Cross-cutting
- Rate limiting: `Microsoft.AspNetCore.RateLimiting` (see §15.7).
- Request size limits: 1 MB JSON default; 25 MB per-file / 50 MB per-request on multipart upload endpoints.
- Idempotency: `Idempotency-Key` header on all POST mutations; stored hashed in `IdempotencyRecords` (key, user_id, request_hash, response_status, response_body, expires_at), composite PK `(key, user_id)`, 24h TTL. Same key + same body = cached response; same key + different body = 409.
- CORS: locked-down origin allow-list. Never `AllowAnyOrigin`.
- Antiforgery: enabled on Blazor Server cookie surfaces; not needed for pure JWT bearer.

---

## 10. Data Model Approach

### 10.1 Strongly-typed IDs

```csharp
public readonly record struct StudyId(Guid Value)
{
    public static StudyId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
```

Wired via global value-converter convention in `ConfigureConventions`:

```csharp
configurationBuilder
    .Properties<StudyId>()
    .HaveConversion<StudyIdConverter>();
```

Or generic helper using reflection over `Domain` types implementing marker `IStronglyTypedId<TKey>`. UUIDv7 for sortable keys → better B-tree behavior.

### 10.2 Owned types
`Address`, `ContactInfo`, `SignatureMeta` configured as `OwnsOne` — collapses into parent table with prefixed columns. Use for tightly-coupled value objects that never live independently.

### 10.3 Base entity

```csharp
public abstract class Entity<TId> where TId : struct
{
    public TId Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public DateTimeOffset UpdatedAt { get; private set; }
    public string UpdatedBy { get; private set; } = default!;
    public byte[] RowVersion { get; private set; } = default!;
    public bool IsDeleted { get; private set; }

    private readonly List<IDomainEvent> _events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _events;
    protected void Raise(IDomainEvent e) => _events.Add(e);
    public void ClearEvents() => _events.Clear();
}
```

`RowVersion` configured as `IsRowVersion()`; Postgres uses `xmin` system column via `UseXminAsConcurrencyToken()`. Update commands check it; conflicts → `Error.Conflict("concurrency")` → HTTP 409.

### 10.4 Soft delete
Global query filter: `modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted)`. Hard-delete reserved for the (out-of-scope) GDPR erasure flow via `IgnoreQueryFilters()`. Audit rows live in a separate immutable table and are never filtered by `IsDeleted`.

### 10.5 Audit interceptor

`AuditingSaveChangesInterceptor : SaveChangesInterceptor`. Key design:
- Audit rows append-only — Postgres rule/trigger raises exception on UPDATE/DELETE (pedagogically interesting; aligns with GxP).
- Snapshot serialization: `System.Text.Json` with `JsonSerializerOptions` respecting `[JsonIgnore]` on sensitive fields.
- Interceptor needs `ICurrentUser` and `IClock` — inject via `DbContext` constructor.
- Tested via integration tests, not unit.

### 10.6 Migration strategy
- Migrations in `PharmaFlow.Infrastructure/Persistence/Migrations/`.
- Local: `dotnet ef migrations add <Name> --project src/PharmaFlow.Infrastructure --startup-project src/PharmaFlow.Api`.
- Prod: **EF bundle** (`dotnet ef migrations bundle`) executed in CD pipeline under higher-privilege `app_admin` SQL principal, manual approval gate. Runtime app uses `app_writer` (no UPDATE/DELETE on audit tables). Privilege separation enforced.
- Never edit a migration after applied to a shared environment — add a new one.

---

## 11. AuthN / AuthZ Design

### 11.1 Identity provider — pick

**v1: ASP.NET Core Identity + custom JWT issuer.** Entra External ID is v1.1 stretch.

| Option | Verdict |
|---|---|
| ASP.NET Core Identity + JWT | **Picked.** Full control, transparent, learning-rich, every layer demoable. |
| Entra External ID | Stretch v1.1. Production-grade but configuration-heavy and opaque for portfolio storytelling. |
| IdentityServer / Duende | Over-engineered + commercial licensing. |
| Auth0 / Clerk | Not Azure; doesn't reinforce AZ-204. |

### 11.2 Password policy

| Setting | Value |
|---|---|
| Min length | 12 (NIST 800-63B aligned) |
| Require upper/lower/digit/non-alpha | true (Identity defaults) |
| Max length | 128 (blocks ReDoS / hash DoS) |
| Hash | PBKDF2-SHA512, 600k iterations (Identity v3 default) |
| History | Last 5 passwords (custom `IPasswordValidator<TUser>`) |
| Expiry | None (NIST stance — explicit design decision) |
| Lockout | 5 failed attempts → 15 min, exponential backoff per IP |
| HIBP breached-password check | Optional v1.1; k-anonymity API at `https://api.pwnedpasswords.com/range/{first5}` |

### 11.3 MFA
- **TOTP** via Identity built-in `IUserTwoFactorTokenProvider`. RFC 6238, 30-sec window, ±1 step skew.
- 10 single-use **recovery codes** at enrolment, hashed at rest.
- **Required for all roles** (including Auditor — sees PHI-shaped data).
- **No SMS MFA** — explicit decision (SIM-swap; NIST deprecated).

### 11.4 Session management

| Token | Lifetime | Storage | Notes |
|---|---|---|---|
| Access JWT | 15 min | In-memory client; `Authorization: Bearer` | RS256; key in Key Vault |
| Refresh token | 14 d sliding, 30 d absolute max | HttpOnly Secure SameSite=Strict cookie (Server) or secure store (WASM) | Opaque random 256-bit; stored hashed |
| Refresh rotation | Every refresh issues new pair; old invalidated | `RefreshToken` table with `ReplacedByTokenId`, `RevokedAt` | Reuse detection: revoked token presented → revoke whole family (token-theft signal) |
| Revocation list | DB `RevokedTokens` keyed by `jti` | Cached lookup (5-min memory cache) | Manual revoke on password change, role change, key rotation |

**Signing-event step-up.** Before any e-signature commit: force fresh credential challenge (password + TOTP) regardless of session age. Issue short-lived (~5 min) signing-scope JWT with `signing_intent` claim bound to target record id and operation. Signing endpoint requires this scope claim.

### 11.5 Roles (recap from §2)
Sponsor, Investigator, Coordinator, Auditor, SystemAdmin.

### 11.6 Authorization model — three layers
1. **Role-based** — `[Authorize(Roles = "Sponsor,Investigator")]` or policy `RequireRole`.
2. **Policy-based** — declarative compound rules (`CanSignProtocol` = `IsInvestigator AND HasFreshMfa AND StepUpScopePresent`).
3. **Resource-based** — `IAuthorizationHandler<Operation, Resource>` checks the participant's `SiteId` is in the user's site claims and `StudyId` is in user's study claims. **Single most important authorization control** — prevents cross-site data leakage.

Resource handlers live in Application layer, invoked at boundary of every read/write of a study-scoped entity.

### 11.7 Claims design

```
sub             = user GUID
name            = display name (audit-log readability)
role            = [Sponsor | Investigator | Auditor | StudyCoordinator | SystemAdmin]
site_id         = ["site-001", "site-014"]
study_id        = ["study-onc-001", ...]
mfa             = true | false
mfa_at          = ISO8601 timestamp of last MFA
amr             = ["pwd","mfa"]   (RFC 8176)
signing_scope   = optional, "<entity>:<id>:<op>" — step-up token only
jti             = token id (for revocation)
iss / aud / exp / iat / nbf
```

site/study claims hydrated from DB at token issuance — **never trusted from client**. Token reissue triggered when membership changes.

---

## 12. Electronic Signature Implementation (Part 11 alignment)

### 12.1 Two distinct identification components (§11.200(a)(1))

- **First signing in continuous session:** password **+** TOTP.
- **Subsequent signings in same continuous session:** at least one component (password re-entry).
- **"Continuous session"** defined: no interactive idle > 5 min, no logout, same client fingerprint, same IP /24. Tracked **server-side** on the signing-scope token.

### 12.2 Signing manifestation (§11.50)

Every signed record displays, on screen and in any printout/export:
1. Printed name of signer.
2. Date and time of execution (UTC stored, local + UTC displayed).
3. Meaning: `Approved | Reviewed | Rejected | Witnessed | Authored`.

Manifestation rendered from the `Signature` row, **not** the user's current state — so a renamed/deactivated user still shows the name as it was at signing time.

### 12.3 Hash binding & signature chain

```
canonical_payload = JCS(  // RFC 8785 JSON Canonicalization Scheme
  {
    entity_type, entity_id, entity_version,
    record_snapshot,        // full field set being signed
    signer_user_id, signer_display_name, signer_role,
    meaning,                // "Approved"
    signed_at_utc,
    previous_signature_hash // null on first sig, else prior Signature.Hash
  }
)
record_hash = SHA-256(canonical_payload)
signature   = HMAC-SHA-256(record_hash, signing_key)   // v1
              // v2: RSA-PSS / Ed25519 detached signature using a per-tenant key in Key Vault
```

- `signing_key` is HMAC key in Key Vault, retrieved via Managed Identity, never leaves API process memory.
- `previous_signature_hash` chains all signatures on the same `(entity_type, entity_id)` so any tamper or reorder is detectable by replay.
- **Verification job:** nightly background `BackgroundService` replays the chain over a sample, emits App Insights metric `signature_chain_integrity_check {pass|fail}`.

### 12.4 Non-repudiation properties
- Signature row **never updatable** (DB-level `DENY UPDATE` + interceptor enforcement).
- Bound via FK with `OnDelete(Restrict)` — cannot delete a signed record (only supersede with new version + new signature).
- Cannot be excised — removal breaks hash chain on subsequent signatures, detected by verifier.
- Cannot be transferred — `signer_user_id` is part of hashed payload; copying yields a record_hash that doesn't match.

### 12.5 Signing flows

**First signing in session:**
```
POST /signing/intent { entityType, entityId, operation, meaning }
  → server validates resource auth, returns signing_intent_id
POST /signing/authenticate { signing_intent_id, password, totpCode }
  → server validates both, issues signing-scope JWT (5 min, single-use)
POST /signing/commit { signing_intent_id, reason }   Authorization: <signing-scope JWT>
  → server: load entity, build canonical payload, hash, sign,
            insert Signature, write AuditEvent, mark intent consumed
  → 201 with signature manifestation block
```

**Subsequent within continuous session:** TOTP omitted in `/authenticate`.

Continuity broken → falls back to two-component flow.

### 12.6 `Signature` schema

| Column | Type | Notes |
|---|---|---|
| Id | GUID PK | |
| EntityType | varchar(64) | e.g. `ProtocolAmendment` |
| EntityId | GUID | |
| EntityVersion | int | row version at signing |
| SignerUserId | GUID FK Users | not nullable |
| SignerDisplayName | varchar(256) | snapshot |
| SignerRole | varchar(64) | snapshot |
| Meaning | varchar(32) | enum |
| Reason | varchar(512) | optional free text |
| SignedAtUtc | datetime2 | UTC, server clock |
| RecordSnapshot | jsonb / nvarchar(max) | canonical JSON of signed fields |
| RecordHash | binary(32) | SHA-256 |
| PreviousSignatureHash | binary(32) NULL | chain pointer |
| SignatureValue | binary(32) or varies | HMAC v1 / detached sig v2 |
| SigningKeyId | varchar(64) | Key Vault key version reference |
| ClientIp | varchar(45) | IPv6-safe |
| UserAgent | varchar(512) | |
| MfaMethod | varchar(32) | "TOTP" |
| ContinuousSession | bit | true if subsequent |
| CorrelationId | GUID | App Insights traceability |

Indexes: `(EntityType, EntityId, SignedAtUtc)`; `(SignerUserId, SignedAtUtc)`. DB-level: `REVOKE UPDATE, DELETE ON Signatures FROM app_writer_role`; only `app_signer_role` can `INSERT`.

---

## 13. Audit Trail Implementation (§11.10(e), ALCOA+)

### 13.1 ALCOA+ mapping

| Attribute | How PharmaFlow satisfies |
|---|---|
| Attributable | UserId, UserDisplay, Role on every event; system actor "system" only for cron jobs, explicitly tagged |
| Legible | Structured JSON, plain-language Action, ChangedFields with before/after, Auditor UI + CSV export |
| Contemporaneous | Captured in same DB transaction as change via EF interceptor |
| Original | Server-generated `OccurredAt`; client timestamps recorded separately |
| Accurate | Hash chain detects tamper; row-level lock on insert; UTC clock from Azure |
| Complete | Captures Create/Update/Delete-marker (soft)/Sign/Read-sensitive (audit views, signed-record exports) |
| Consistent | Sequence number `SeqNo bigint identity` per partition; chronological order preserved |
| Enduring | Primary DB + nightly export to immutable Blob (legal-hold container) |
| Available | Indexed for query; Auditor endpoint p95 < 1s for 90-day window |

### 13.2 `AuditEvent` table schema

| Column | Type | Notes |
|---|---|---|
| Id | bigint identity PK | monotonic |
| OccurredAt | datetime2 (UTC) | server clock |
| EntityType | varchar(64) | |
| EntityId | varchar(64) | string handles composite keys |
| Action | varchar(32) | `Create | Update | Delete | SoftDelete | Sign | ReadSensitive | Login | LoginFailed | RoleChange | KeyRotation` |
| ChangedFields | jsonb / nvarchar(max) | `{"field": {"before": ..., "after": ...}}`; null for Read |
| UserId | GUID NULL | null for anonymous (login attempt) |
| UserDisplay | varchar(256) | snapshot |
| Role | varchar(64) | snapshot |
| Reason | varchar(512) NULL | required for Update/Delete on GxP entities |
| IpAddress | varchar(45) | |
| UserAgent | varchar(512) | |
| CorrelationId | GUID | matches App Insights `operation_Id` |
| Hash | binary(32) | SHA-256 of canonical row + PreviousHash |
| PreviousHash | binary(32) NULL | chain pointer (per-partition) |
| Signature | binary(varies) NULL | optional HMAC of Hash for sealed-event surface |

Indexes: `(EntityType, EntityId, OccurredAt)`, `(UserId, OccurredAt)`, `(OccurredAt)`.

### 13.3 Immutability at DB layer

Two SQL principals on the same database:

```
app_writer  — used by API for normal traffic.
              Permissions on AuditEvents: SELECT, INSERT.
              Explicitly DENY UPDATE, DELETE.

app_admin   — used only for migrations & emergency repair.
              Stored in Key Vault, manual gate, alert on use.
```

In Azure SQL: both Entra-integrated managed identities. Even SQL injection that bypasses EF cannot tamper with audit rows under writer principal. Defence-in-depth talking point.

### 13.4 Hash chain

Per partition (v1 = single global chain):

```
PreviousHash = last AuditEvent.Hash (locked SELECT under same tx)
canonical    = JCS({ Id, OccurredAt, EntityType, EntityId, Action,
                     ChangedFields, UserId, Role, Reason,
                     IpAddress, UserAgent, CorrelationId,
                     PreviousHash })
Hash         = SHA-256(canonical)
```

Insertion serialised via app-level lock. ~hundreds of writes/sec on a single partition is fine for v1. Discuss in interview: partition by tenant scales horizontally.

Verifier job: weekly background hosted service replays chain → metric `audit_chain_integrity_check`.

### 13.5 EF Core `SaveChangesInterceptor`

```
class AuditInterceptor : SaveChangesInterceptor
  override SavingChangesAsync(ctx):
    foreach entry in ctx.ChangeTracker.Entries():
      if entry.Entity is IAuditedEntity:
        capture { Action, EntityType, EntityId, ChangedFields (Modified properties only) }
        require Reason claim if Action in (Update, SoftDelete)
        enqueue into ctx-scoped pending-audit list
  override SavedChangesAsync(ctx):
    for each pending audit:
      compute hash chain (locked)
      INSERT into AuditEvents
    commit (same tx as entity changes — atomicity)
```

### 13.6 Reason-for-change

DTOs for any `PUT/PATCH/DELETE` on `IAuditedEntity` require a `reason` field validated by FluentValidation (min 5, max 512). Enforced at API boundary (rejects 400) and at interceptor (defence-in-depth — throws if missing).

### 13.7 Auditor endpoints

| Endpoint | Auth | Behaviour |
|---|---|---|
| `GET /audit?entityType=&entityId=&from=&to=&userId=&action=` | Auditor | Paginated 50/page; logs `ReadSensitive` itself |
| `GET /audit/{id}` | Auditor | Single event; logs `ReadSensitive` |
| `GET /audit/export?from=&to=&format=csv` | Auditor | Streamed CSV; rate-limited 1/min; logs `ReadSensitive` with `RowCount` |
| `GET /audit/integrity?from=&to=` | Auditor / Admin | On-demand chain replay; returns `{ ok, brokenAt? }` |

CSV columns: Id, OccurredAt (ISO8601 UTC), EntityType, EntityId, Action, UserDisplay, Role, Reason, IpAddress, ChangedFields. Hash columns excluded (operational noise) but available via `?includeHashes=true`.

### 13.8 Why audit table, not full event sourcing

Audit table is the engineered minimum that meets §11.10(e). Event sourcing over-satisfies, introduces event-schema versioning hell, projection rebuild tooling. Document trade-off in README ADR. **Event sourcing is overkill for v1.**

---

## 14. Secrets & Data Protection

### 14.1 Local dev
- `dotnet user-secrets init` per project; secrets in `~/.microsoft/usersecrets/<id>/secrets.json`.
- `.env` files forbidden. Add `gitleaks` pre-commit hook in Month 3.
- `appsettings.Development.json` only non-secret config.

### 14.2 Azure secrets — Key Vault + Managed Identity

| Secret | Storage | Consumer |
|---|---|---|
| `Sql-ConnectionString` | KV secret (or Entra-auth MI to SQL — preferred) | API |
| `Jwt-SigningKey-Current` / `-Previous` | KV key (RSA 2048 or EC P-256) | API |
| `Signature-HmacKey-v1` | KV secret, versioned | Signing service |
| `OpenAI-ApiKey` (Project 2 / DocChat) | KV secret | DocChat API |
| `Storage-ConnectionString` | Not stored — use MI to Blob with `Storage Blob Data Contributor` | API |
| `AppInsights-ConnectionString` | KV secret (or app config — non-sensitive enough) | API |

App Service config references KV via:
```
@Microsoft.KeyVault(SecretUri=https://kv-pharmaflow-dev.vault.azure.net/secrets/Jwt-SigningKey-Current/)
```
Resolves at startup via system-assigned MI which has `Key Vault Secrets User` role.

**Rotation:** manual quarterly v1; document procedure with `kid` handling so old JWTs validate during the window via `Jwt-SigningKey-Previous` slot.

### 14.3 Data Protection API

```csharp
services.AddDataProtection()
  .PersistKeysToAzureBlobStorage(blobUri, credential: ManagedIdentity)
  .ProtectKeysWithAzureKeyVault(kvKeyUri, credential: ManagedIdentity);
```

Without this, restart invalidates antiforgery, refresh-token cookie protection, etc.

### 14.4 At-rest

| Layer | Mechanism |
|---|---|
| Azure SQL data files | TDE on by default with service-managed key (BYOK with KV is v1.1) |
| Blob storage | SSE on by default (AES-256, MS-managed key) |
| Key Vault | Standard tier secrets v1; HSM-backed Premium documented as upgrade |
| Backups | Azure SQL automatic, geo-redundant, encrypted |

### 14.5 In-transit
- HTTPS only; App Service "HTTPS Only" flag on; TLS 1.2 min, prefer 1.3.
- HSTS `max-age=31536000; includeSubDomains; preload` (only after custom domain + cert proven stable).
- Outbound `IHttpClientFactory` with TLS 1.2 min and certificate-revocation checking on.
- Internal SQL `Encrypt=True; TrustServerCertificate=False`.

### 14.6 PII handling
- **Synthetic data only.** README banner. Seed with `Bogus`-generated obviously-fake names ("Test Subject 001", DOB always 2000-01-01).
- UI footer banner red: "DEMO ENVIRONMENT — DO NOT ENTER REAL PATIENT DATA".
- Validators reject obvious PII patterns in free-text (regex SSN/NHS/email — soft warning + log).
- Data classification table in README:

| Field | Class | Stored? |
|---|---|---|
| Subject ID (synthetic) | Pseudonymous identifier | yes |
| Real name, DOB, address | PII / PHI | **never** |
| Investigator name & email (staff user) | Personal data of staff user | yes — Identity store |

### 14.7 GDPR-shaped endpoints (signal, not requirement)

| Endpoint | Behaviour |
|---|---|
| `GET /me/export` | JSON of user's identity record + signing events. Audited as `ReadSensitive`. |
| `POST /me/delete-request` | Marks user `DeletionRequested`. Real deletion **forbidden** for users with audit/signing history (immutability beats erasure — FDA/ICH wins over GDPR for trial records — document the collision). Pseudonymise display name to `Redacted User <hash>`, retain audit immutability. |

---

## 15. Application Security Baseline

### 15.1 OWASP Top 10 (2021) mapping

| OWASP | Mitigation |
|---|---|
| A01 Broken Access Control | Resource-based authz handlers (§11.6); deny-by-default; integration tests assert cross-site forbidden |
| A02 Cryptographic Failures | TLS 1.2+, HSTS, AES-256 at rest, JWT RS256, HMAC in KV, no plaintext secrets in repo |
| A03 Injection | EF Core parameterisation; FluentValidation at boundary; Razor output-encoding; `JsonSerializerOptions` strict |
| A04 Insecure Design | STRIDE pass (§16); ADRs; deny-by-default; least-privilege SQL principals |
| A05 Security Misconfiguration | Prod forces secure defaults: error details off, dev exception page off, Swagger off in prod (or behind admin auth) |
| A06 Vulnerable & Outdated Components | Dependabot weekly; `dotnet list package --vulnerable --include-transitive` failing CI on High/Critical |
| A07 Auth Failures | Identity v3 hash, lockout, MFA required, refresh rotation with reuse detection |
| A08 Data Integrity Failures | Hash-chained audit & signature; CI signs container images (cosign v1.1); branch protection on `main` |
| A09 Logging & Monitoring | Serilog → App Insights, correlation IDs, alerts on auth failures, audit-log read events tracked |
| A10 SSRF | No outbound URL fetch from user input v1; if added, strict allow-list + DNS pinning + block private IP ranges |

### 15.2 Input validation
FluentValidation per-DTO at boundary. Server-authoritative; client validation UX only. Reject unknown JSON fields (`JsonSerializerOptions.UnmappedMemberHandling = Disallow`) — prevent mass-assignment. Length caps everywhere; range checks; strict enum validation.

### 15.3 Output encoding
Razor/Blazor framework defaults HTML-encode; never `@((MarkupString)userInput)` on untrusted content. CI grep for `MarkupString` requires comment justification.

### 15.4 SQL injection
EF Core only. Raw SQL prohibited except in migrations and audit-integrity verifier (review-required, parameterised). PR template checkbox.

### 15.5 CSRF
- Pure JWT bearer endpoints (Authorization header) → no CSRF risk; antiforgery would be inert.
- Blazor Server uses cookie auth → enable antiforgery; form posts have `<AntiforgeryToken />`.
- Document the model split.

### 15.6 CORS

```csharp
services.AddCors(o => o.AddPolicy("api", p => p
  .WithOrigins("https://pharmaflow-web.azurestaticapps.net",
               "https://pharmaflow.<custom>.dev")
  .WithMethods("GET","POST","PUT","DELETE","PATCH")
  .WithHeaders("Authorization","Content-Type","X-Correlation-Id")
  .AllowCredentials()
  .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));
```

No `*`. No `AllowAnyHeader`. Integration test asserts unknown origin rejected.

### 15.7 Rate limiting

`Microsoft.AspNetCore.RateLimiting` policies:

| Policy | Limit | Partition | Endpoints |
|---|---|---|---|
| `global` | 1000 req/min | IP | all |
| `auth` | 10 req/min | IP | `/auth/*` |
| `signing` | 30 req/min | UserId | `/signing/*` |
| `export` | 5 req/hour | UserId | `/audit/export`, `/me/export` |

429 with `Retry-After`.

### 15.8 Security headers (middleware)

| Header | Value |
|---|---|
| `Content-Security-Policy` | `default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; connect-src 'self' https://*.applicationinsights.azure.com; frame-ancestors 'none'; base-uri 'self'; form-action 'self'` (tune for Blazor mode) |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` |
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | `geolocation=(), microphone=(), camera=(), payment=()` |
| `Cross-Origin-Opener-Policy` | `same-origin` |
| `Cross-Origin-Resource-Policy` | `same-site` |

CSP report-only mode for first 2 weeks via `Content-Security-Policy-Report-Only` + `report-to` endpoint.

### 15.9 File upload (documents for sign-off)
- Per-file 25 MB cap; per-request 50 MB.
- Allow-listed content types: `application/pdf`, `image/png`, `image/jpeg`.
- **Magic-number sniff on server** (PDF `%PDF-`, PNG `89 50 4E 47`, JPEG `FF D8 FF`) before accepting; reject if Content-Type and bytes disagree.
- Stripped-filename storage: `<guid>.pdf`; original captured separately.
- Stored in Blob with private access; access via short-lived (10 min) **user-delegation SAS**; never account-key SAS.
- AV scanning out-of-scope v1; document Defender for Storage as v1.1. Mark uploads `quarantined: true` for 5 min as placeholder.

### 15.10 Dependency scanning

| Tool | Role | Stage |
|---|---|---|
| GitHub Dependabot | Automated PRs for vuln updates | Repo setting, weekly |
| `dotnet list package --vulnerable --include-transitive` | Fail CI on High/Critical | CI build job |
| GitHub CodeQL | SAST on .NET | CI security job, weekly + PR |
| `gitleaks` pre-commit hook | Block secret commits | Dev machine |
| `trivy fs` on Dockerfile | Container base-image CVEs | CI container job |

---

## 16. Threat Model — STRIDE

### 16.1 Asset: Audit Log

| STRIDE | Threat | Control |
|---|---|---|
| Tampering | Insider with DB access edits `ChangedFields` to hide an unblinding event | DB-level `DENY UPDATE/DELETE` to app principal; hash-chain verifier detects post-hoc edits |
| Repudiation | Sponsor disputes that they viewed unblinded data | Auditor reads logged as `ReadSensitive`; row + IP + UA preserved |
| Information Disclosure | Auditor exports CSV containing IPs to external party | Export rate-limited; export action audited; data-handling policy |
| DoS | Attacker spams login attempts to inflate audit table | Rate limiting; partitioned audit; cold-storage offload v1.1 |

### 16.2 Asset: Signed Record

| STRIDE | Threat | Control |
|---|---|---|
| Spoofing | Phish Investigator MFA secret | TOTP requires device possession; step-up before signing; recovery codes hashed; WebAuthn v2 |
| Tampering | Edit record after signing without invalidating signature | Hash includes record snapshot; verification fails; chain breaks |
| Repudiation | Investigator claims "I didn't sign that" | Signature row contains AMR snapshot, IP, UA, MFA method, server timestamp |
| Information Disclosure | Exported PDF leaks signed record | DLP out-of-scope v1; manifestation watermark "Demo / Synthetic Data" |
| DoS | Mass invalid signing-intent creation | `signing` rate-limit policy |
| Elevation of Privilege | Coordinator signs as Investigator | Authz policy `CanSign` denies Coordinator; integration test asserts |

### 16.3 Asset: Signing Key (HMAC v1 / future RSA in KV)

| STRIDE | Threat | Control |
|---|---|---|
| Spoofing | Forge identity to retrieve key | Managed Identity only; KV access scoped to API's MI; Defender for KV alerts |
| Tampering | Replace key with attacker-known value | KV soft-delete + purge protection; alert on key version create |
| Information Disclosure | Key leaks via memory dump or log | Never logged; `[Sensitive]` redaction in Serilog; App Service "Always On" with managed-disk encryption |
| Repudiation | Admin denies rotating key | KV diagnostic logs → Log Analytics, 90-day retention |
| Elevation of Privilege | App principal granted unnecessary KV permissions | Least-privilege role: `Key Vault Crypto User` (sign/verify only), not `Crypto Officer` |

### 16.4 Per role

| Role | Top threat | Control |
|---|---|---|
| System Admin | Account takeover → grants self Investigator, signs records | Separation-of-duty (admin cannot self-grant clinical roles); break-glass admin in separate tenant v1.1; alert on role-change events |
| Sponsor | Account takeover → approves protocol amendment | MFA + step-up for signing; signing rate-limit |
| Investigator | Coerced/social-engineered signing | MFA possession factor on different device; reason captured |
| Auditor | Read-only exfil of audit log | Export rate-limit; auditor reads audited; cannot delete trail of own reads |
| Coordinator | Misuse of elevated read of Investigator's data | Resource-based authz limits to assigned site; reads logged |

---

## 17. Azure Deployment Topology

### 17.1 Resources (single region — recommend `westeurope`; document choice)

| Resource | Name | SKU | Purpose | ~Cost/mo |
|---|---|---|---|---|
| Resource Group | `rg-pharmaflow-dev` | — | container | $0 |
| App Service Plan | `asp-pharmaflow-dev` | **B1 Linux** | API + Blazor Server | $13 |
| Web App (API) | `app-pharmaflow-api-dev` | runs on B1 | .NET 10 API | $0 in plan |
| Web App (Blazor) | `app-pharmaflow-web-dev` (Server) **or** Static Web Apps Free (WASM) | — | UI | $0 |
| Postgres Flexible Server | `psql-pharmaflow-dev` | B1ms, 32 GB | OLTP | ~$13 |
| ↳ alt | Azure SQL Basic 5 DTU | — | OLTP | $5 |
| Storage Account | `stpharmaflowdev` | Standard LRS | Documents, DataProtection keys, audit cold-archive | $1–3 |
| Key Vault | `kv-pharmaflow-dev` | Standard | secrets, JWT key, signing key | $0.03/op |
| Application Insights | `appi-pharmaflow-dev` | workspace-based | telemetry | first 5 GB free |
| Log Analytics workspace | `log-pharmaflow-dev` | PAYG | logs | first 5 GB/mo free |
| Azure Container Registry | `crpharmaflowdev` | **Basic** | image registry | $5 |
| Front Door / WAF | — | — | **deferred v1.1** | — |
| Custom domain + managed cert | `pharmaflow.<owner-domain>` | App Service Managed Cert | optional polish | $0 + domain |

**Frontend hosting decision:** Blazor Server on same App Service plan as API. Cookie auth + antiforgery story cleaner; SignalR one less moving part; AZ-204 covers App Service deeply. Add WASM mode in Month 5 alongside DocChat.

### 17.2 Cost ceiling
**Target: $20–30/mo** with free tiers exhausted. Best case ~$25/mo (SQL Basic, no SWA, App Insights under 5 GB). Switch to SQL Serverless with auto-pause for bursty dev — drops below $10. Document a `kill-switch` script for stop/start.

Set Azure budget alert at **$40** with action group emailing owner — screenshot in README.

### 17.3 Networking (v1)
- Public endpoints with App Service Access Restrictions (allow-list owner IP for `/health/ready` etc).
- SQL firewall: Azure services + owner IP only.
- KV firewall: App Service outbound IPs (or Service Endpoint) + owner IP.
- v1.1 stretch: Private Endpoints + VNet integration. Document as upgrade path.

### 17.4 Identity wiring
App Service has **system-assigned managed identity** enabled. MI granted:
- `Key Vault Secrets User` on `kv-pharmaflow-dev`
- `Key Vault Crypto User` on the signing key
- `Storage Blob Data Contributor` on `stpharmaflowdev` (documents container)
- `Storage Blob Data Contributor` on data-protection-keys container (separate, scoped)
- SQL: Entra user, granted `db_datareader` + `db_datawriter` + custom role for `INSERT`-only on `AuditEvents` and `Signatures`
- `Monitoring Metrics Publisher` on App Insights

ACR: GitHub Actions OIDC federated identity has `AcrPush`; App Service has `AcrPull`.

---

## 18. CI/CD Pipeline (GitHub Actions)

### 18.1 Workflows

```
.github/workflows/
  ci.yml         (on PR + push to main)
  cd-dev.yml     (on push to main, after CI green)
  codeql.yml     (weekly + PR security scan)
.github/dependabot.yml
```

### 18.2 `ci.yml` jobs (parallel where possible)

| Job | Steps | Fails build on |
|---|---|---|
| `lint-format` | `dotnet format --verify-no-changes` | formatting drift |
| `restore-build` | `dotnet restore` (NuGet cache) → `dotnet build -c Release --no-restore` | warnings as errors |
| `unit-test` | `dotnet test --filter Category!=Integration` + Coverlet → upload `coverage.cobertura.xml` | failure or Domain coverage < 70% |
| `integration-test` | Spin up Postgres + Azurite via Testcontainers → `dotnet test --filter Category=Integration` | any failure |
| `security-deps` | `dotnet list package --vulnerable --include-transitive` | High/Critical |
| `security-codeql` | CodeQL .NET pack | High alert |
| `docker-build` | `docker build` → `trivy image` scan | High/Critical CVE |
| `coverage-report` | merge cobertura → reportgenerator → PR comment | informational |

Branch protection on `main`: required checks = all of the above + 1 review.

### 18.3 `cd-dev.yml`

```
permissions:
  id-token: write     # OIDC to Azure
  contents: read

steps:
  - checkout
  - login-azure (azure/login@v2 OIDC; client-id, tenant-id, subscription-id; no secret)
  - docker login to ACR (azure/docker-login@v2 OIDC)
  - docker push <tag = git sha>
  - download efbundle artifact, run with prod connection string from KV (managed-identity-resolved)
  - manual approval gate ("environment: dev-deploy")
  - app service deploy: az webapp config container set --image=<acr/sha>
  - smoke test: curl /health/ready until 200, fail after 60s
```

### 18.4 OIDC federated identity (no secrets)
App Registration `pharmaflow-github-deployer` with federated credentials for:
- `repo:<owner>/pharmaflow:ref:refs/heads/main`
- `repo:<owner>/pharmaflow:pull_request`
- `repo:<owner>/pharmaflow:environment:dev-deploy`

RBAC scoped to `rg-pharmaflow-dev` only (`Contributor`). **No PATs, no client secrets in GitHub.** Strong interview talking point.

### 18.5 Migrations strategy

| Option | Verdict |
|---|---|
| First-run apply (`db.Database.Migrate()` at startup) | No — concurrent races, no privilege separation |
| **EF bundle (`dotnet ef migrations bundle`) + CD step** | **Yes** — works under CI/CD identity; failure surfaces before app starts |
| `dotnet ef migrations script --idempotent` reviewed manually | Production-style stretch |

V1: bundle on deploy with manual approval gate. Migration runs under higher-privilege `app_admin` SQL principal; runtime app uses `app_writer`. Privilege separation enforced.

---

## 19. Observability

### 19.1 Logging
- Serilog only. Sinks: Console (JSON, App Service log stream) + Application Insights (via `Serilog.Sinks.ApplicationInsights`).
- Enrichers: `FromLogContext`, `WithMachineName`, `WithEnvironmentName`, custom `WithCorrelationId` (W3C `traceparent`).
- Levels: Information default; Warning for `Microsoft.*` and EF Core; Debug only via runtime config flag.
- **Sensitive-data redaction** custom `ILogEventEnricher` strips properties named `password`, `token`, `secret`, `signatureValue`, `recordSnapshot`. Unit-test the redactor.
- Structured properties only — no string interpolation. `_logger.LogInformation("Study {StudyId} signed by {UserId}", studyId, userId)`.

### 19.2 Tracing
OpenTelemetry with Azure Monitor exporter (App Insights). Instrumentation: AspNetCore, HttpClient, EF Core, Azure SDK (KV, Blob), Service Bus (when added). W3C TraceContext propagation. Logs include `TraceId` + `SpanId`.

### 19.3 Health checks

| Endpoint | Auth | Checks |
|---|---|---|
| `GET /health/live` | anonymous | process up |
| `GET /health/ready` | anonymous (consider IP-restricted) | DB, KV, Blob, App Insights reachability |
| `GET /health/startup` | anonymous | migrations applied marker, signing key available |

App Service health probe → `/health/live`; deployment smoke test → `/health/ready`.

### 19.4 Custom metrics (`System.Diagnostics.Metrics` → App Insights)

| Metric | Type | Dimensions |
|---|---|---|
| `pharmaflow.signings.completed` | Counter | role, meaning, entity_type |
| `pharmaflow.signings.failed` | Counter | reason (auth, validation, conflict) |
| `pharmaflow.audit.events.written` | Counter | action, entity_type |
| `pharmaflow.audit.chain.integrity.fail` | Counter | partition |
| `pharmaflow.documents.uploaded` | Counter | content_type |
| `pharmaflow.auth.login.failed` | Counter | reason (password, mfa, lockout) |
| `pharmaflow.auth.stepup.completed` | Counter | role |

### 19.5 Alerts

| Alert | Threshold | Severity | Action |
|---|---|---|---|
| API p95 latency | > 1s over 5 min | Sev 3 | email |
| Exception rate | > 1% over 5 min | Sev 2 | email |
| Dependency failures (SQL/KV/Blob) | > 5/min | Sev 2 | email |
| `audit.chain.integrity.fail` | > 0 | Sev 1 | email + incident note |
| `auth.login.failed` per IP | > 50 / 10 min | Sev 3 | email |
| Azure budget | > 80% of $40 | Sev 3 | email |
| Key Vault unauthorized access | any | Sev 1 | email (Defender for KV) |

### 19.6 Dashboards (App Insights workbook + Azure dashboard)

Panes:
1. **Health** — live request count, p50/p95/p99, failed requests, dependency call durations.
2. **Auth** — login success/fail, MFA challenge volume, step-up volume, lockouts.
3. **Signing & Audit** — signings per hour by meaning, audit events per minute, chain integrity status, latest 20 signings table.
4. **Errors** — exception count by type, top 10 failing operations, slowest dependencies.
5. **Cost** — App Service CPU/memory, Postgres usage, Storage transactions, AI ingestion volume.

---

## 20. Compliance Mapping

> Honesty rule: this is a **portfolio simulation**. Mapping below states what PharmaFlow *demonstrates*. Full GxP compliance requires QMS, validation protocols (URS/FS/DS/IQ/OQ/PQ), supplier audits, periodic review, signed-off SOPs that no portfolio project will produce.

### 20.1 21 CFR Part 11 — Subpart B (Electronic Records)

| Control | Citation | Approach | Honesty note |
|---|---|---|---|
| Validation of systems | §11.10(a) | xUnit + Testcontainers integration tests; CI gate; signed release tags | No formal IQ/OQ/PQ |
| Generate accurate complete copies | §11.10(b) | Audit + record export endpoints in JSON & CSV; signature manifestation included | Demonstrated |
| Protection enabling accurate retrieval | §11.10(c) | TDE, geo-redundant backup default, hash-chained audit | Retention policy not formal |
| Limit access to authorised individuals | §11.10(d) | Identity, MFA, RBAC + resource-based authz | Demonstrated |
| Audit trail | §11.10(e) | `AuditEvent` table with hash chain, before/after, ALCOA+ | Strong demo |
| Operational system checks (sequencing) | §11.10(f) | Signing-intent → authenticate → commit forced order | Demonstrated |
| Authority checks | §11.10(g) | Policy + resource handlers per operation | Demonstrated |
| Device checks | §11.10(h) | UA + IP captured; not enforced | **Not implemented** — note |
| Education, training, experience | §11.10(i) | N/A — solo dev | Cannot simulate |
| Written policies of accountability | §11.10(j) | README + ADRs | Light proxy |
| Document controls (system docs) | §11.10(k) | README + ADRs in repo with git history | Light proxy |
| Open systems | §11.30 | N/A — closed system v1 | Documented |
| Signature manifestations | §11.50 | Name + datetime + meaning rendered with the record | Demonstrated |
| Signature/record linking | §11.70 | Hash binding + chain + FK Restrict | Strong demo |

### 20.2 21 CFR Part 11 — Subpart C (Electronic Signatures)

| Control | Citation | Approach |
|---|---|---|
| Unique to one individual | §11.100(a) | Identity, no shared accounts, FK to user GUID |
| Identity verification before issuing | §11.100(b) | Admin grants role; out-of-band confirmation in real deployment (manual step documented) |
| Two distinct identification components | §11.200(a)(1) | Password + TOTP first sig; password subsequent in continuous session |
| Used only by genuine owner | §11.200(a)(2) | T&Cs at first login (modal); UA/IP capture |
| Loss management of tokens | §11.200(b) | Recovery codes; admin reset workflow |
| Password controls | §11.300(a)–(d) | Identity v3 hashing, lockout, history, strength |

### 20.3 GAMP 5

| Element | Approach |
|---|---|
| Software category | Custom-developed (Cat 5) |
| V-model artefacts | Light proxy: README → user stories → ADRs → tests; **no formal URS/FS/DS** v1 |
| Risk-based testing | One-page risk register in README listing high-risk features (signing, audit) and corresponding integration tests |
| Periodic review | Out-of-scope; mention in README |
| Supplier assessment | N/A — single developer |

### 20.4 Honest framing for interviews

> "PharmaFlow demonstrates the technical controls a Part-11-aligned system needs — hash-chained audit trail with EF interceptor, two-component electronic signatures with continuous-session handling, separation-of-duty roles, and immutability enforced both at the application and database principal levels. It does not pretend to be validated software: there is no QMS, no IQ/OQ/PQ, and no SOP-controlled change management. The portfolio purpose is to prove I can implement the controls correctly and reason about where the simulation ends."

---

## 21. Testing Strategy

### 21.1 Unit — `PharmaFlow.Tests.Unit`
Stack: **xUnit v3** + **FluentAssertions** + **NSubstitute**.

What:
- **Domain:** every aggregate method, factory, value-object equality, invariants. Pure tests, no DI. Target 200+ tests.
- **Application handlers:** mock repository / `IAppDbContext` / ports with NSubstitute.
- **Validators:** `validator.TestValidate(command)`.
- **Pipeline behaviors:** unit-test `ValidationBehavior` and `TransactionBehavior` with fake `RequestHandlerDelegate`.

Coverage: **80% line / 70% branch on Domain + Application**. Don't chase coverage on Infrastructure or Api in unit tests.

### 21.2 Integration — `PharmaFlow.Tests.Integration`
Stack: `WebApplicationFactory<Program>` + **Testcontainers.PostgreSql** + xUnit collection fixtures.

Pattern: shared `IAsyncLifetime` collection fixture spins up Postgres once, applies migrations. Each test wrapped in transaction that rolls back on dispose (or Respawn between tests).

What:
- One happy-path + one auth-failure case per endpoint.
- Audit interceptor end-to-end: mutate study → assert audit row with correct old/new JSON.
- Concurrency: two parallel updates → assert one returns 409.
- Idempotency: same `Idempotency-Key` twice → second response identical, no double-write.
- Cross-site denial: assert resource-based handler blocks Investigator from another site's data.

Coverage: every endpoint hit at least once; every pipeline behavior path exercised.

**Do NOT mock the DB in integration tests.**

### 21.3 E2E — Playwright for .NET
Smoke flow: log in as Sponsor → create study → invite Investigator → upload document → Investigator signs → Auditor views audit log.
Run against Blazor Web App + API in `WebApplicationFactory` (in-process) or full Docker Compose. Defer until frontend past MVP.

### 21.4 What NOT to test
- DbContext configurations directly (covered indirectly).
- Mapperly mappers (compile-time generated).
- Serilog configuration.

---

## 22. Non-Functional Requirements

### 22.1 Latency targets
- p50 read endpoint: **< 80 ms** in-region
- p95 read endpoint: **< 250 ms**
- p95 write endpoint: **< 500 ms** (includes audit interceptor + tx commit)
- p99 anywhere: **< 1500 ms** before alerting

Realistic for App Service B1 + Postgres B1ms in same region with EF projections + indexed lookups. Aspirations not SLOs — measure with App Insights once deployed.

### 22.2 EF Core / DB
- Connection pool: Npgsql default 100/process; drop to 50 on B1 to stay under Postgres B1ms 50-conn limit.
- `EnableRetryOnFailure(maxRetryCount: 3)` — covers transient blips.
- Compiled queries only after profiling identifies hot read query >1000 rps.
- Indexes: every FK, every soft-delete column, every column in pagination `ORDER BY`.

### 22.3 Pagination
- Default page size **20**; max **100** (reject >100 with 400).
- **Cursor-based** for unbounded lists (audit log, participant list, document history). Cursor = opaque-encoded `(SortKey, Id)` tuple, URL-safe base64.
- **Offset-based** acceptable for short admin lists (≤ 1000 rows).
- Response envelope: `{ items: [...], nextCursor: "...", hasMore: true }`. Don't return total counts on cursor responses.

### 22.4 Caching
- **Output caching** on small number of read endpoints clearly cacheable with seconds-of-staleness tolerance (Auditor study list, 60s TTL, varied by user/role). Audit log: never cached.
- **No distributed cache** v1. Document Redis as v2 lever when single read endpoint hits >50 rps.
- **Memory cache** for small reference data (roles, study-status enums): `IMemoryCache`, 5-min sliding.

Principle: **cache nothing until you've measured what's slow.**

### 22.5 Concurrency
- Optimistic via `RowVersion` (Postgres `xmin`) on all aggregates.
- Conflicts → `Error.Conflict("concurrency")` → HTTP 409 with `Last-Modified`/`ETag` retry hint.
- Pessimistic locking only inside `SaveChanges` transaction (`FOR UPDATE`) for rare cases (signature counters, sequence-bound business numbers).

### 22.6 Idempotency
All POST mutations require `Idempotency-Key` header (client-generated GUID). Stored in `idempotency_records` table. Behavior covered §9.5. Exempt: GET, DELETE (HTTP-idempotent), `/auth/login` (different replay model).

---

## 23. Build & Tooling

### 23.1 `Directory.Build.props` (root of `/sandbox/PharmaFlow/`)

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors>CS1591</WarningsNotAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <AnalysisMode>AllEnabledByDefault</AnalysisMode>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>
</Project>
```

### 23.2 `Directory.Packages.props` — Central Package Management

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="MediatR" Version="12.4.1" />
    <PackageVersion Include="FluentValidation" Version="11.11.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.11.0" />
    <PackageVersion Include="Riok.Mapperly" Version="4.1.1" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    <PackageVersion Include="Serilog.AspNetCore" Version="9.0.0" />
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.10.0" />
    <PackageVersion Include="Azure.Monitor.OpenTelemetry.Exporter" Version="1.4.0" />
    <PackageVersion Include="xunit.v3" Version="1.0.0" />
    <PackageVersion Include="FluentAssertions" Version="7.0.0" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="Testcontainers.PostgreSql" Version="4.1.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
    <!-- ...etc. Pin versions current at scaffold time. -->
  </ItemGroup>
</Project>
```

Per-project `.csproj`: `<PackageReference Include="MediatR" />` (no version).

### 23.3 `.editorconfig`
Place at `/sandbox/PharmaFlow/.editorconfig`. Microsoft modern template plus:
- `csharp_style_namespace_declarations = file_scoped:warning`
- `dotnet_style_qualification_for_*  = false:warning` (no `this.`)
- `csharp_new_line_before_open_brace = all`
- 4-space indent C#, 2-space `.csproj`/JSON/YAML.
- Naming-rule violations as errors (interface `I*`, async methods `*Async`, private fields `_camelCase`).

### 23.4 `.gitignore`
Root `/Users/sergeybozhko/Coding/C-sharp/learning/.gitignore` is sufficient — no second `.gitignore` inside PharmaFlow. Aspire usage adds `*.aspire-manifest.json` to project-local ignore.

### 23.5 Format & test scripts
- `dotnet format` — CI gate; pre-push git hook locally.
- `dotnet test --collect:"XPlat Code Coverage"` — Coverlet → Cobertura.
- `reportgenerator` global tool for HTML coverage locally; CI uploads Cobertura.

---

## 24. Module Boundaries — Feature Folders

**Decision: feature-folders, not technical-folders.**

Inside `PharmaFlow.Application`:

```
PharmaFlow.Application/
├── Common/
│   ├── Behaviors/
│   │   ├── LoggingBehavior.cs
│   │   ├── ValidationBehavior.cs
│   │   ├── TransactionBehavior.cs
│   │   ├── IdempotencyBehavior.cs
│   │   └── AuditBehavior.cs
│   ├── Interfaces/         (IAppDbContext, ICurrentUser, IClock, IDocumentStorage, IAuditWriter)
│   ├── Mapping/            (Mapperly partial mapper hubs)
│   └── Pagination/
├── Studies/
│   ├── Commands/
│   │   ├── CreateStudy/
│   │   │   ├── CreateStudyCommand.cs
│   │   │   ├── CreateStudyHandler.cs
│   │   │   ├── CreateStudyValidator.cs
│   │   │   └── CreateStudyResponse.cs
│   │   └── CloseStudy/...
│   ├── Queries/
│   │   ├── GetStudyById/...
│   │   └── ListStudies/...
│   └── Dtos/
├── Participants/
├── Documents/
├── Signatures/
├── Auditing/
└── Identity/
```

Mirror in `PharmaFlow.Api/Endpoints/` and `PharmaFlow.Domain/`.

**Trap to avoid:** don't let `Common/` become a dumping ground. If two features share a class, prefer duplication until duplication hurts.

---

## 25. Implementation Sequencing (12 weeks × 10 hrs)

Aligned to `LearningPlan.md` Months 2–4.

| Phase | Weeks | Deliverable |
|---|---|---|
| 0. Scaffold | 1 | Solution, 7 projects, `Directory.Build.props`, `Directory.Packages.props`, CI green, hello-world endpoint. |
| 1. Domain core + persistence | 2–3 | Domain entities (per §3), `AppDbContext`, migrations, audit interceptor (no chain yet), integration test passes against Testcontainers. |
| 2. CQRS + first feature | 4–5 | MediatR + behaviors wired; `Studies` feature end-to-end (CRUD + cursor pagination + audit verified). |
| 3. Auth | 6 | Identity + JWT, role policies, refresh-rotation + revocation, rate limiting, security headers, Auditor read-only enforcement. Resource-based authz integration tests. |
| 4. Documents + Blob + eSig | 7–8 | Document upload to Azurite/Blob; Signature schema + hash binding + chain; signing flows (intent → authenticate → commit); MFA TOTP enrolment + step-up. |
| 5. Audit query API + Auditor UI | 9 | Audit query endpoints (paginated, filterable); chain verifier hosted service; Blazor pages consuming them. |
| 6. Frontend MVP | 10–11 | Blazor pages for Sponsor / Investigator / Coordinator / Auditor flows; Playwright smoke. |
| 7. Observability + deploy | 12 | OTel → App Insights; deploy to Azure App Service + Postgres + KV + Blob; dashboards; ZAP manual run; README compliance section + ADRs; Loom walkthrough. |

Each phase has at least one named integration test that's screenshot-worthy for README.

---

## 26. Critical Files (paths to be created)

Solution-level:
- `/Users/sergeybozhko/Coding/C-sharp/learning/sandbox/PharmaFlow/PharmaFlow.sln`
- `/Users/sergeybozhko/Coding/C-sharp/learning/sandbox/PharmaFlow/Directory.Build.props`
- `/Users/sergeybozhko/Coding/C-sharp/learning/sandbox/PharmaFlow/Directory.Packages.props`
- `/Users/sergeybozhko/Coding/C-sharp/learning/sandbox/PharmaFlow/global.json`
- `/Users/sergeybozhko/Coding/C-sharp/learning/sandbox/PharmaFlow/.editorconfig`
- `/Users/sergeybozhko/Coding/C-sharp/learning/sandbox/PharmaFlow/README.md`

Domain:
- `src/PharmaFlow.Domain/Common/Entity.cs`
- `src/PharmaFlow.Domain/Common/Result.cs`, `Error.cs`
- `src/PharmaFlow.Domain/Studies/Study.cs` (aggregate + state machine)
- `src/PharmaFlow.Domain/Documents/DocumentVersion.cs` (versioning + supersession)
- `src/PharmaFlow.Domain/Compliance/SignatureRecord.cs`
- `src/PharmaFlow.Domain/Compliance/AuditEvent.cs`

Application:
- `src/PharmaFlow.Application/Common/Behaviors/{Logging,Validation,Transaction,Idempotency,Audit}Behavior.cs`
- `src/PharmaFlow.Application/Common/Interfaces/{IAppDbContext,ICurrentUser,IClock,IDocumentStorage,IAuditWriter}.cs`
- `src/PharmaFlow.Application/Studies/Commands/CreateStudy/*.cs`

Infrastructure:
- `src/PharmaFlow.Infrastructure/Persistence/AppDbContext.cs`
- `src/PharmaFlow.Infrastructure/Persistence/Auditing/AuditingSaveChangesInterceptor.cs`
- `src/PharmaFlow.Infrastructure/Persistence/Migrations/`
- `src/PharmaFlow.Infrastructure/Signing/SigningService.cs` (hash binding, chain, KV HMAC)

API:
- `src/PharmaFlow.Api/Program.cs` (composition: auth, authz policies, CORS, rate limiting, security headers, OpenTelemetry, health checks)
- `src/PharmaFlow.Api/Authorization/ResourceAuthorizationHandlers.cs`
- `src/PharmaFlow.Api/Endpoints/{Studies,Participants,Documents,Signatures,Auditing,Identity}/*.cs`
- `src/PharmaFlow.Api/Common/ResultExtensions.cs` (Result → ProblemDetails)

CI/CD:
- `.github/workflows/ci.yml`
- `.github/workflows/cd-dev.yml`
- `.github/workflows/codeql.yml`
- `.github/dependabot.yml`

Docs:
- `docs/adr/` (decisions: MediatR vs Mediator, Postgres vs SQL, Blazor mode, no-event-sourcing)
- `docs/compliance/part11-mapping.md`
- `docs/compliance/alcoa-plus-mapping.md`

Infra (v1, az CLI scripts):
- `infra/scripts/01-create-rg.sh`
- `infra/scripts/02-create-keyvault.sh`
- `infra/scripts/03-create-postgres.sh`
- `infra/scripts/04-create-storage.sh`
- `infra/scripts/05-create-app-service.sh`
- `infra/scripts/06-create-app-insights.sh`
- `infra/scripts/07-grant-managed-identity-roles.sh`

---

## 27. CV / Hiring-Signal Mapping

Each entry: feature → CV bullet → signal.

| Feature | CV bullet (drop-in) | Signal |
|---|---|---|
| Immutable hash-chained audit trail | "Implemented append-only, SHA-256-chained audit trail satisfying 21 CFR Part 11 §11.10(e), with tamper-evidence verification endpoint and DB-level `DENY UPDATE/DELETE` on app principal." | Compliance literacy + crypto primitives + defence-in-depth understanding. |
| Content-bound electronic signatures | "Designed Part 11–compliant eSignature subsystem (§11.50, §11.70, §11.200) with content-hash binding, signature-meaning capture, and credential re-authentication on signing." | Knows actual Part 11 clauses, not just buzzwords. |
| Document versioning with effective-date supersession | "Built controlled-document workflow (Draft → InReview → Effective → Superseded) with deterministic effective-date queries — analogous to Veeva Vault QualityDocs version control." | GxP document-control + name-checks industry vendor. |
| Consent capture bound to current ICF version | "Engineered Informed Consent capture enforcing consent against the currently-Effective IRB-approved ICF version, with re-consent triggers on protocol amendment." | Clinical operations literacy. |
| Soft-delete + reason-for-change everywhere | "Enforced ALCOA+ data integrity: soft-delete only, mandatory reason-for-change on every regulated mutation, before/after state preservation." | ALCOA+ as working principle. |
| Resource-based RBAC + segregation of duties | "Implemented scoped RBAC (system / study / site) with segregation of duties between System Administration and clinical-business roles, all role changes audit-logged and signed." | Enterprise access control + GxP segregation. |
| Auditor read-only role + audit export | "Delivered Auditor read-only role with PDF/CSV audit-trail export and hash-chain verification report, designed for inspection-readiness." | Knows what an FDA inspection looks like. |
| Strongly-typed IDs + state machines | "Modelled domain with strongly-typed identifiers (StudyId, ParticipantId, etc.) and explicit state machines for Study, Site, Document, Participant lifecycles." | Modern .NET + DDD. |
| Pseudonymised subject model | "Deliberately pseudonymised subject data model (SubjectNumber + Initials + YearOfBirth) to scope out PHI handling — documented design decision aligning with privacy-by-design." | Senior judgement: knows what *not* to build. |
| OIDC federated GitHub→Azure deploy | "GitHub Actions OIDC federated identity to Azure — no PATs, no client secrets, RBAC-scoped to the resource group only." | Modern cloud security maturity. |
| Managed Identity to KV/Blob/SQL | "App Service system-assigned Managed Identity wired to Key Vault, Blob, and SQL — runtime never holds connection strings or signing keys outside Key Vault." | Cloud-native identity competence. |
| Refresh token rotation with reuse detection | "Refresh-token rotation with reuse detection — token theft revokes the whole token family." | Practical OAuth/OIDC patterns. |
| EF Core SaveChangesInterceptor + DB-enforced immutability | "Hash-chained audit log via EF Core SaveChangesInterceptor + DB-level `DENY UPDATE/DELETE` to the app principal — even SQL injection cannot tamper." | Defence-in-depth talking point. |
| Honest compliance framing | "Demonstrate the technical controls; do not pretend to be validated software — no QMS, no IQ/OQ/PQ. Portfolio simulation, not a regulatory submission." | Senior judgement; pharma hiring managers reward this. |
| Domain vocabulary used correctly | "Modelled Sponsor / CRO / PI / Coordinator / CRA / Subject roles and Protocol / ICF / SOP / CRF / SAE artefacts per ICH-GCP E6(R3)." | Won't embarrass yourself in a Veeva / Medidata / IQVIA / Parexel interview. |

**Interview elevator pitch:**
> "PharmaFlow is a clinical study tracker I built to demonstrate regulated-software literacy. It models the Sponsor / Investigator / Coordinator / Auditor roles and the Study / Site / Subject / Document / Consent domain. The compliance core is a 21 CFR Part 11–style electronic signature with credential re-auth, content-hash binding, and signature-meaning capture, plus a hash-chained immutable audit trail with a tamper-evidence verifier. I deliberately scoped out real EDC, real PHI, and HL7/FHIR — those are vertical industries, and the goal was to demonstrate I understand GxP document control, ALCOA+, and Part 11, not to rebuild Veeva."

---

## 28. Verification (how to test the project end-to-end)

When PharmaFlow v1 is built, verify by:

1. **Local build & test**
   - `dotnet build sandbox/PharmaFlow/PharmaFlow.sln -c Release` exits 0 with zero warnings (TreatWarningsAsErrors).
   - `dotnet test` runs all projects; unit + integration green; coverage on Domain ≥ 80%.
   - `dotnet format --verify-no-changes` passes.

2. **Local run with Docker dependencies**
   - `docker compose up postgres azurite` brings up dependencies.
   - `dotnet run --project src/PharmaFlow.Api` boots; `/health/ready` returns 200.
   - `dotnet run --project src/PharmaFlow.Web` boots Blazor on adjacent port.

3. **Functional smoke (manual / Playwright script)**
   - Log in as seeded Sponsor (`sponsor@demo`, password from user-secrets, MFA via stored TOTP secret).
   - Create a Study; upload a Protocol PDF; assign two Sites + PIs; activate Study with eSignature; assert signature manifestation block visible.
   - Log in as Investigator; sign Protocol + ICF for site; assert dashboard updates.
   - Log in as Coordinator; register a subject; capture consent against current ICF; assert consent record references currently-Effective ICF version.
   - Log in as Auditor; view audit trail for study; export CSV; hit `/audit/integrity` — assert `{ ok: true }`.
   - Tamper test (manual): with `app_admin` principal, attempt `UPDATE AuditEvents SET ChangedFields = '...' WHERE Id = X`; re-run integrity verifier; assert `{ ok: false, brokenAt: <id> }`.

4. **CI green**
   - Push to feature branch; PR triggers `ci.yml`; all jobs green; CodeQL no High alerts; Dependabot no High/Critical CVEs; coverage report attached as PR comment.

5. **CD to Azure dev**
   - Merge to `main`; `cd-dev.yml` runs; OIDC login succeeds; container pushed to ACR; EF bundle migration applied (manual approval gate); App Service container restart; smoke test against `https://<app>.azurewebsites.net/health/ready` returns 200.
   - Open public URL; demo flow works; App Insights live metrics show traffic; signing event creates a `pharmaflow.signings.completed` data point.

6. **Compliance evidence pack (for README + interviews)**
   - Screenshot of audit trail in Auditor UI showing before/after on a real change.
   - Screenshot of signature manifestation on a signed Protocol with printed name + UTC timestamp + meaning.
   - Screenshot of `/audit/integrity` returning `ok: true` after normal operation; another after deliberate tamper.
   - Screenshot of App Insights dashboard panes 1–5 (§19.6).
   - Screenshot of GitHub Actions run with all jobs green and OIDC login step.
   - Loom walkthrough (5 min): scaffold → architecture diagram → live demo → "what I cut and why".

7. **Open Questions to resolve before scaffold (Phase 0)**
   - Confirm Postgres vs Azure SQL pick (default: Postgres).
   - Confirm Blazor Auto vs Server (default: Auto for net10, fall back to Server if Auto-mode learning curve adds friction).
   - Confirm `westeurope` vs other Azure region (cost + latency from KG).
   - Confirm GitHub repo name and visibility (public from day 1 is the right call for portfolio).

---

## 29. Decision Log (ADRs to write during build)

Each lives as a one-page Markdown file in `docs/adr/NNNN-title.md`:

1. ADR-0001: Postgres over Azure SQL.
2. ADR-0002: MediatR 12.x over Mediator (martinothamar).
3. ADR-0003: Mapperly over AutoMapper.
4. ADR-0004: Hand-rolled `Result<T>` over FluentResults.
5. ADR-0005: Blazor Web App (Auto) over React.
6. ADR-0006: ASP.NET Core Identity + JWT over Entra External ID for v1.
7. ADR-0007: Audit table with hash chain over event sourcing.
8. ADR-0008: HMAC signing v1; RSA-PSS / Ed25519 deferred to v2.
9. ADR-0009: Single-tenant v1 with multi-tenant seam (TenantId shadow + global filter).
10. ADR-0010: No microservices / no message broker for v1.
11. ADR-0011: Out-of-scope list (no real PII/PHI/EDC/FHIR/HL7) — deliberate scope decisions.
12. ADR-0012: GDPR right-to-erasure vs Part 11 immutability — Part 11 wins; pseudonymise on deletion request.

---

## 30. Open Questions for Owner (clarify before scaffold)

1. **Path A confirmed?** This spec assumes Path A (.NET + AI for pharma). Confirm before kickoff.
2. **8–10 hrs/week sustainable?** Spec phasing assumes ~120 hrs total. If lower, cut scope further (drop Coordinator role or document workflow first).
3. **Public repo from day 1?** Strong yes recommended (portfolio + commit graph signal).
4. **Custom domain?** Optional; cosmetic only.
5. **Pharma industry contacts who could review?** Even a 30-min review with someone in pharma SaaS is gold for vocabulary checks.