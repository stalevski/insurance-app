# GitHub Copilot instructions

This repository uses a portable, tool-agnostic context file: **[`AGENTS.md`](../AGENTS.md)** at the
repository root. **Read it first** - it is the single source of truth for architecture, conventions,
and build/test commands.

Quick summary (see `AGENTS.md` for full detail):

- **.NET 10** modular monolith; ASP.NET Core minimal APIs; EF Core + **SQLite**.
- Strict **contract separation**: `SourceContracts` → `Mappers` → `CanonicalContracts` →
  `Services/Flows` → `Responses`; plus snapshots (read models), domain events, and outbox.
- **Conventions:** one class per file; namespaces match folders; **file-scoped namespaces**; prefer
  `var`; thin endpoints (logic in `Services/Flows`); inject `TimeProvider` (never `DateTime.UtcNow`
  in logic); `decimal` for money; migrations are generated, not hand-edited.
- **Tests:** **NUnit 4** (not xUnit), AAA style, `FakeTimeProvider` for time.
- **Before done:** `dotnet format --verify-no-changes`, `dotnet build` (no new warnings),
  `dotnet test` green.
- Known, intentionally-unfixed issues are catalogued in `docs/project/KNOWN_ISSUES.md`.
- Multi-device work: read and update `PROGRESS.md`.
