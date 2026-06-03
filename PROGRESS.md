# PROGRESS.md

> **Multi-device continuity file.** This project is worked on from more than one machine (not at the
> same time). Read this first to see where things stand, then update the "Current status" and
> "Next steps" sections at the end of each working session so the next device/agent has context.
> For architecture and conventions, see [`AGENTS.md`](AGENTS.md).

_Last updated: 2026-06-03_

## Vision

A .NET 10 insurance ingest/transformation modular monolith that doubles as a **QA testing ground**
and is being grown into a **deployable product** with a Blazor UI, intended for deployment on a
Docker-capable VPS (e.g. MaxHosting MK VPS tier).

## Roadmap (phased)

- [x] **Phase 0 — AI-agnostic context + continuity.** `AGENTS.md`, `.github/copilot-instructions.md`,
  `.windsurf/rules/{architecture,testing}.md`, and this `PROGRESS.md`.
- [ ] **Phase 1 — Document bugs.** `docs/KNOWN_ISSUES.md` cataloguing the 14 issues found during
  analysis (documentation only; no code changes).
- [ ] **Phase 2 — Per-line-of-business risk models.** Lightweight: one `CanonicalRiskRequest` +
  optional typed detail objects (Property/Cyber/Motor/Liability) + per-LOB strategy replacing the
  `ProductCode.Contains(...)` string checks in `RiskFlowService`.
- [ ] **Phase 3 — Blazor Server UI.** Create submissions/quotes/policies, list + detail views,
  status **flow diagram** (Mermaid from domain events), read-only **DB browser**.
- [ ] **Phase 4 — Self-contained packaging.** Dockerfile + short VPS deployment guide.

## Current status

- Baseline build is green (`dotnet build -c Release` succeeds; 38 pre-existing warnings, no errors).
- Test framework confirmed: **NUnit 4** (+ `FakeTimeProvider`), ~32 test files.
- Note: the codebase has evolved beyond the original README in places — there are real risk
  entities and a `Persistence/Migrations/Phase10a_AddRiskEntities` migration. Verify current
  persistence shape before Phase 2 model work.
- **Working on now:** Phase 0 (this commit) → starting Phase 1.

## Decisions locked in

- **Stay on .NET.** No language migration. UI is **Blazor Server** (single language, single host,
  self-contained) — not React/Vue, to minimise toolchains.
- **Bugs:** document now (Phase 1), fix in a later separate pass.
- **Risk models:** lightweight (additive typed detail objects + strategy), not a subclass hierarchy.
- **DB browser:** read-only, available in all environments, no auth yet (revisit before real
  production exposure — recommend gating behind Development or a feature flag).
- **Hosting target:** MaxHosting MK **VPS** (root access required for Docker; shared/cPanel hosting
  cannot run the app).

## Next steps

1. Create `docs/KNOWN_ISSUES.md` (Phase 1).
2. Confirm current risk persistence (entities/migrations) before Phase 2.
3. Begin Phase 2 risk-model work.

## Open questions / to confirm with hosting provider

- MaxHosting VPS: which Linux OS options, and is Docker installable out of the box? (Open a support
  ticket; root access is advertised, so almost certainly yes.)
