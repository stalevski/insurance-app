---
trigger: glob
globs: tests/**
description: Test conventions for the insurance integration platform
---

# Testing conventions (applies to tests/**)

- **Framework is NUnit 4** (not xUnit). Use `[TestFixture]` / `[Test]`, `Assert.That(...)`.
- Follow **Arrange-Act-Assert**; one logical behavior per test; descriptive test names.
- **Time:** use `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) - never wall-clock
  time. Code under test should accept an injected `TimeProvider`.
- Prefer testing **flow/services** directly (they hold the business logic) over endpoints.
- **HTTP-endpoint + Blazor-UI tests** live in `tests/InsuranceIntegration.Api.IntegrationTests`:
  use `WebApplicationFactory<Program>` via `ApiTestBase`/`SeededApiTestBase` for the API, and
  **bUnit** with a stub `IUiGateway` for Razor pages. See `docs/guides/TESTING.md` §5.7-5.8.
  Tag new integration fixtures with `[Category("Api")]` / `[Category("Ui")]` (the `Api` category is
  inherited from `ApiTestBase`) so the `--filter "Category=..."` subsets stay accurate.
- For EF Core, use SQLite (in-memory or a temp file) as in existing tests; keep tests isolated.
- New behavior or bug fixes must come with a test. Keep the suite green:
  `dotnet test InsuranceIntegration.sln`.
- Don't introduce new build warnings; code style is enforced at build time.
