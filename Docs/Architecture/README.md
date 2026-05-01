# Architecture Documentation

Living-document set covering the structural views of PharmaFlow at increasing levels of zoom. Every diagram is Mermaid (renders in GitHub + most IDEs) so the source-of-truth is checked-in text, not a binary.

| # | View | C4 level | Audience | Update trigger |
|---|---|---|---|---|
| [01](01-system-context.md) | System context | L1 | Stakeholders, hiring reviewers | New external system or actor |
| [02](02-container-diagram.md) | Container diagram | L2 | Developers onboarding | New runnable / deployable unit |
| [03](03-module-dependencies.md) | Module dependencies | between L2/L3 | Anyone touching `.csproj` files | New project or `<ProjectReference>` change |
| [04](04-domain-model.md) | Domain model | L3 | Domain-layer contributors | New aggregate / typed ID / interface |

## Conventions

- **Mermaid only.** No PlantUML, no static images. Trade-off: GitHub renders Mermaid but not every IDE does (Rider needs the markdown plugin).
- **Source of truth lives in spec sections.** Each diagram cites which spec section it derives from. If they disagree, *spec wins* — fix the diagram.
- **Drift is documented inline**, not hidden. See `03-module-dependencies.md` § "Drift vs spec" for current known-deltas between intent and code.
- **Future levels** (`05-persistence.md`, `06-cqrs-pipeline.md`, `07-deployment.md`) will land in the sprints that build them. Empty placeholder files are *not* added preemptively — the folder grows when content has substance.

## When to read which doc

- "What is this system and who uses it?" → `01-system-context.md`
- "How does a request flow end-to-end?" → `02-container-diagram.md`
- "Can I add a NuGet package to `Domain`?" (spoiler: probably not) → `03-module-dependencies.md`
- "What does an aggregate look like?" → `04-domain-model.md`
- "Why did we pick X over Y?" → `Docs/ADRs/`
- "What are the entities and rules of the business domain?" → `Docs/PharmaFlow — Technical Specification.md` §3 / §5
