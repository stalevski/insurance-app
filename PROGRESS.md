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
- [x] **Phase 1 — Document bugs.** `docs/KNOWN_ISSUES.md` cataloguing the verified issues found during
  analysis (documentation only; no code changes).
- [x] **Phase 2 — Per-line-of-business risk models.** `LineOfBusiness` enum + optional typed detail
  objects (Property/Cyber/Motor/Liability) on `CanonicalRiskRequest` + `IRiskTypeProfile` strategy
  per LOB replacing the `ProductCode.Contains(...)` checks in `RiskFlowService`. 12 new tests, 4
  example JSON files.
- [ ] **Phase 3 — Blazor Server UI.** Create submissions/quotes/policies, list + detail views,
  status **flow diagram** (Mermaid from domain events), read-only **DB browser**.
- [ ] **Phase 4 — Self-contained packaging.** Dockerfile + short VPS deployment guide.

## Current status

- Build green (`dotnet build -c Release`); **all 128 tests pass** (was 116; +12 from Phase 2).
- `dotnet format --verify-no-changes` flags **one pre-existing** generated migration
  (`Phase10a_AddRiskEntities.cs`: charset + IDE0161). Not introduced by recent work; left as-is
  (generated migration). All new Phase 0-2 files are format-clean.
- Test framework: **NUnit 4** (+ `FakeTimeProvider`).
- **Working on now:** Phase 3 (Blazor Server UI).

## Decisions locked in

- **Stay on .NET.** No language migration. UI is **Blazor Server** (single language, single host,
  self-contained) — not React/Vue, to minimise toolchains.
- **Bugs:** document now (Phase 1), fix in a later separate pass.
- **Risk models:** lightweight (additive typed detail objects + strategy), not a subclass hierarchy.
- **DB browser:** read-only, available in all environments, no auth yet (revisit before real
  production exposure — recommend gating behind Development or a feature flag).
- **Hosting target:** MaxHosting MK **VPS** (root access required for Docker; shared/cPanel hosting
  cannot run the app).
- **No language/stack migration (hosting analysis).** Hosting cost is driven by the VPS, not the
  language: a Linux VPS that runs Node/Python also runs .NET for the same price. A JS/TS or Blazor
  WASM static frontend can be hosted free, but the backend (background outbox dispatcher, EF +
  SQLite, persistent connections) still needs the same always-on VPS, so total cost is unchanged.
  Avoiding the VPS entirely requires re-architecting to serverless + a hosted DB (works in .NET
  too, usually costs *more*); the only genuinely-free no-VPS path is Cloudflare Workers + D1, which
  is JS-native but would mean discarding the whole architecture/tests/UI. Conclusion: **keep .NET +
  Blazor Server**; minimise cost via a small trimmed container (Phase 4), not a rewrite.

## Next steps

1. Phase 3 — scaffold Blazor Server UI (recommend a separate `InsuranceIntegration.Web` project or
   add Blazor to the API host): create forms (submission/quote/policy) posting to existing
   endpoints; list + detail views over snapshots; Mermaid status flow diagram from domain events;
   read-only DB browser.
2. Phase 4 — Dockerfile + short MaxHosting VPS deployment guide.

## Open questions / to confirm with hosting provider

- MaxHosting VPS: which Linux OS options, and is Docker installable out of the box? (Open a support
  ticket; root access is advertised, so almost certainly yes.)
