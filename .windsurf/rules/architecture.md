---
trigger: always_on
description: Core architecture and conventions for the insurance integration platform
---

# Architecture & conventions (always on)

The portable source of truth is `AGENTS.md` at the repo root — read it for full detail. Key points:

- **.NET 10** modular monolith; ASP.NET Core minimal APIs; EF Core + **SQLite** (`integration.db`,
  auto-migrated on startup).
- **Contract separation is mandatory:** `SourceContracts` → `Mappers` → `CanonicalContracts` →
  `Services/Flows` → `Responses`. Plus snapshots (read models), domain events (append-only log),
  and a transactional outbox.
- **Conventions:**
  - One public class/record per file; file name matches type; namespaces match folders.
  - File-scoped namespaces; usings outside the namespace; prefer `var`.
  - Thin endpoints in `Endpoints/` — business logic goes in `Services/Flows/`.
  - Composition over inheritance; small interfaces registered in
    `Configuration/ServiceRegistration.cs`.
  - Inject `TimeProvider`; never use `DateTime.UtcNow` directly in logic.
  - Money is `decimal`; keep premium/installment totals reconciled.
  - Migrations are generated via `dotnet ef`, never hand-edited.
- **Before finishing a change:** `dotnet format --verify-no-changes`, `dotnet build` (no new
  warnings), `dotnet test` green.
- Documented-but-unfixed issues live in `docs/project/KNOWN_ISSUES.md`. Multi-device continuity: `PROGRESS.md`.
