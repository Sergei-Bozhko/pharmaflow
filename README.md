# PharmaFlow

> Clinical-trial study tracker — portfolio simulation on .NET 10 + Azure.

![CI](https://img.shields.io/badge/CI-pending-lightgrey)
![Build](https://img.shields.io/badge/build-pending-lightgrey)
![License](https://img.shields.io/badge/license-MIT_(tbc)-lightgrey)

<!-- CI badge replaced by the real GitHub Actions badge in PFL-010. -->

## What this is

PharmaFlow is a clinical-trial study-tracker web app I'm building to demonstrate the technical controls a 21 CFR Part 11 / ALCOA+ regulated system actually needs — hash-chained audit trail, two-factor electronic signatures, separation-of-duty roles, immutability — on a modern .NET 10 + Azure stack. It's a portfolio simulation, not a real product: no QMS, no validated SOPs, no formal IQ/OQ/PQ. This is Project 1 of 3 in a 6-month rebuild aimed at a Senior .NET / pharma-tech role.

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
- **xUnit v3** + **Testcontainers** + **FluentAssertions** + **NSubstitute**

## Architecture

Clean Architecture across seven projects: `Domain` (pure C# aggregates and value objects, no framework references), `Application` (CQRS commands / queries / handlers / pipeline behaviors), `Infrastructure` (EF Core, Blob adapter, JWT issuer, OTel wiring), `Api` (Minimal API endpoint groups, composition root), `Web` (Blazor Server), plus unit and integration test projects. Source-code dependencies point inward; runtime adapters are registered in DI from the composition root.

Full layer map and dependency rules: see [Technical Specification §7](<Docs/PharmaFlow — Technical Specification.md>).

## Run locally

> **TODO** — populated in Sprint 5 once the Studies feature is wired end-to-end.

Prerequisites:

- **.NET 10 SDK** (version pinned in `global.json`)
- **Docker** — for PostgreSQL via `docker run postgres`, and Azurite for Blob Storage emulation
- **`gh` CLI** (optional — for PR / Actions interaction from the terminal)

## Compliance

PharmaFlow is a **portfolio simulation, not validated software.** There is no quality management system, no IQ/OQ/PQ protocols, no signed-off SOPs, no supplier audit, no periodic review. What it does demonstrate is the technical-control surface a Part-11-aligned system needs: hash-chained audit trail via an EF Core `SaveChangesInterceptor`, two-component electronic signatures with continuous-session handling, separation-of-duty roles enforced at both application and database principal level, and immutability of signed records.

Honest framing: the project proves I can implement the controls correctly and reason about where the simulation ends. Full mapping (21 CFR Part 11 Subparts B and C, ALCOA+, GAMP 5): see [Technical Specification §20](<Docs/PharmaFlow — Technical Specification.md>).

## Documents

| Document | Path |
|---|---|
| Technical Specification | [`Docs/PharmaFlow — Technical Specification.md`](<Docs/PharmaFlow — Technical Specification.md>) |
| Architecture Decision Records | [`Docs/ADRs/`](Docs/ADRs/) |

## License

MIT — to be confirmed before public showcase.
