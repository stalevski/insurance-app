# AGENTS.md

> Portable, tool-agnostic project context. Read by GitHub Copilot, Cursor, Windsurf/Cascade,
> Claude, and other AI agents. Keep this file the single source of truth for how this codebase
> works. Tool-specific files (`.github/copilot-instructions.md`, `.windsurf/rules/*`) should point
> here rather than duplicating it.

## What this project is

A **SaaS insurance ingest and transformation platform**, built as a **modular monolith**. It
receives source-specific insurance messages, normalizes them into platform-owned **canonical
contracts**, runs lifecycle-aware processing, persists consolidated read-model **snapshots**, and
emits outbound messages for downstream consumers. It currently doubles as a **QA testing ground**
and is on a path to becoming a deployable product.

## Tech stack

- **Runtime:** .NET 10 (LTS)
- **Web:** ASP.NET Core minimal APIs (thin endpoints)
- **Persistence:** Entity Framework Core 10 + **SQLite** (single-file DB, `integration.db`)
- **Migrations:** EF Core code-first; applied automatically on startup (`Program.cs`)
- **API docs:** OpenAPI/Swagger (`/swagger` in Development), plus JSON Schema endpoints
- **Tests:** **NUnit 4** + NUnit3TestAdapter + coverlet (coverage) + `FakeTimeProvider`
  (`Microsoft.Extensions.TimeProvider.Testing`)
- **Frontend:** Blazor (added in the UI phase; same .NET host, no separate JS toolchain)

## Architecture: layered model separation

Source contracts and canonical contracts and outbound responses are **explicitly separated**.

```
SourceContracts ──Mappers──> CanonicalContracts ──Flow services──> Responses
                                     │                  │
                                     │                  ├─> Snapshots (read models)
                                     │                  ├─> Domain events (append-only log)
                                     │                  └─> Outbox (reliable downstream emit)
```

- **SourceContracts/** — source-facing DTOs (Polaris, QuoteForge, BindPoint, generic envelope).
- **Mappers/** — translate source DTOs into canonical contracts (anti-corruption layer).
- **CanonicalContracts/** — platform-owned normalized models used internally.
- **Services/Flows/** — business logic / orchestration (Risk, Claim, Billing, Compliance).
- **Responses/** — outbound processed models (e.g. `FinalRiskResponse`, `IngestReceipt`).
- **Snapshots/** + **Services/Snapshots/** — consolidated read models per policy/quote key.
- **Events/** + **Persistence/DomainEventEntity** — domain event log; snapshots are rebuildable.
- **Services/Outbox/** — transactional outbox + hosted dispatcher.
- **Endpoints/** — thin minimal-API endpoints; delegate to services, return typed `Results`.
- **Persistence/** — `IntegrationDbContext`, entity types, `RowVersionInterceptor` (optimistic
  concurrency).

## Conventions (follow these)

- **One public class/record per file**; file name matches the type.
- **Namespaces match folder structure.**
- **File-scoped namespaces** (`namespace X;`) — enforced as a warning.
- **Using directives outside** the namespace.
- Prefer **`var`**.
- **Thin endpoints:** no business logic in `Endpoints/`; delegate to a service.
- **Business logic lives in `Services/Flows/`** (and related service folders), isolated for unit
  testing.
- **Composition over inheritance.** Prefer small interfaces + DI registration in
  `Configuration/ServiceRegistration.cs`.
- **Time:** never use `DateTime.UtcNow` directly in logic — inject `TimeProvider` and use
  `FakeTimeProvider` in tests. (Some legacy code still violates this; see `docs/KNOWN_ISSUES.md`.)
- **Money:** use `decimal`; round deliberately and keep installment/premium totals reconciled.
- **Migrations are generated, not hand-edited** (use `dotnet ef migrations add`).
- Code style is enforced at build time (`EnforceCodeStyleInBuild=true`,
  `AnalysisLevel=latest-recommended` in `Directory.Build.props`).

## Build / test / run commands

```bash
# Restore
dotnet restore InsuranceIntegration.sln

# Format check (CI fails if this is not clean)
dotnet format InsuranceIntegration.sln --verify-no-changes --severity warn

# Build
dotnet build InsuranceIntegration.sln -c Release

# Test (NUnit)
dotnet test InsuranceIntegration.sln

# Run the API (serves Swagger at /swagger in Development)
dotnet run --project src/InsuranceIntegration.Api
```

## Definition of done for a change

1. `dotnet format --verify-no-changes` is clean.
2. `dotnet build` succeeds (don't add new warnings).
3. `dotnet test` is green; new behavior has NUnit tests (AAA style, `FakeTimeProvider` for time).
4. Conventions above are respected; contract layers stay separate.

## Where things live (quick map)

| Need | Location |
|------|----------|
| HTTP endpoints | `src/InsuranceIntegration.Api/Endpoints/` |
| DI registration | `src/InsuranceIntegration.Api/Configuration/ServiceRegistration.cs` |
| Business logic | `src/InsuranceIntegration.Api/Services/Flows/` |
| Canonical models | `src/InsuranceIntegration.Api/CanonicalContracts/` |
| Source DTOs + mappers | `src/InsuranceIntegration.Api/SourceContracts/`, `Mappers/` |
| DB context + entities | `src/InsuranceIntegration.Api/Persistence/` |
| Read models | `src/InsuranceIntegration.Api/Snapshots/`, `Services/Snapshots/` |
| Blazor Server UI | `src/InsuranceIntegration.Api/Components/`, `wwwroot/`, `Services/Ui/` |
| Tests | `tests/InsuranceIntegration.Api.Tests/` |
| Known bugs (documented, unfixed) | `docs/KNOWN_ISSUES.md` |
| Project progress / continuity | `PROGRESS.md` |

## Working across multiple devices

This project may be worked on from different machines (not simultaneously). **Read `PROGRESS.md`
first** to see current state and the next step, and **update it at the end of a working session**
so the next device/agent has context.
