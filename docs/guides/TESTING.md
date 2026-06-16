# Testing Guide

How to run, extend, and manually exercise the test suite for **InsuranceIntegration.Api**.

## 0. Two test tiers: `dev` (white-box) and `qa` (black-box)

Tests are split into two independently-runnable tiers under `tests/`:

| Tier | Location | Stack | Style | What it proves |
|---|---|---|---|---|
| **Dev** | `tests/dev/` | .NET 10 + NUnit 4 | **White-box**, in-process | Internal units, services, persistence, HTTP pipeline, and Blazor component render branches — with controlled/stubbed inputs. |
| **QA** | `tests/qa/` | TypeScript + Playwright | **Black-box**, against the running app | The deployed API + UI as a user/integrator sees them: real HTTP, real browser (Chromium/Firefox/WebKit), real seeded data, plus accessibility. |

The two tiers are **complementary, not duplicative** — the dev tier asserts internal decomposition and edge-case render branches the black-box tier cannot reach (e.g. empty states, null props, exception types), while the QA tier catches real wiring, cross-browser, and accessibility issues the in-process stubs hide. See [§9 Two-tier coverage map & reduction review](#9-two-tier-coverage-map--reduction-review).

This guide documents the **dev tier** in §1–§8. The **QA tier** has its own self-contained tooling and README at `tests/qa/README.md`; §9 summarizes how the two relate.

## 1. Dev-tier test stack

The dev tier (`tests/dev/`) has **two test projects**:

- `tests/dev/InsuranceIntegration.Api.Tests` - unit + service/persistence integration tests (the bulk of the suite).
- `tests/dev/InsuranceIntegration.Api.IntegrationTests` - **HTTP-endpoint integration tests** (the API hosted in-process) and **Blazor-UI component tests** (added 2026-06-15).

- **Framework**: [NUnit 4](https://docs.nunit.org/) across both projects (constraint model: `Assert.That(...)`)
- **Test runner**: `dotnet test` via `Microsoft.NET.Test.Sdk` + `NUnit3TestAdapter`
- **Coverage**: `coverlet.collector` is available for producing Cobertura reports
- **Persistence in tests**: EF Core 10 + `Microsoft.Data.Sqlite` in-memory - `DataSource=:memory:` in the unit project; a unique shared-cache in-memory database per `WebApplicationFactory` fixture in the integration project. No external DB required.
- **HTTP-endpoint tests**: `Microsoft.AspNetCore.Mvc.Testing` hosts the API in-process via `WebApplicationFactory<Program>` (`Infrastructure/InsuranceApiFactory.cs`) so tests issue real `HttpClient` requests against the running pipeline (routing, middleware, API-key auth, JSON serialization).
- **Blazor-UI tests**: [bUnit](https://bunit.dev/) renders Razor components against a stub `IUiGateway` (`Ui/UiGatewayStub.cs`) - no browser required.
- **Test-output logging**: the integration host pipes **Warning+** log output to the running NUnit test's output (`Infrastructure/TestContextLoggerProvider.cs`), so a server-side exception behind a failing request surfaces in that test's report; green runs stay quiet (Info-level SQL is filtered out).
- **Categories**: integration fixtures are tagged with NUnit `[Category]` - `Api`, `Ui`, and `Smoke` - so subsets can be run with `--filter` (see §2). The `Api` category sits on `ApiTestBase` and the `Ui` category on `UiPageTestBase`, each inherited by their derived fixtures.
- **Time-controlled tests**: `Microsoft.Extensions.TimeProvider.Testing` provides `FakeTimeProvider` for advancing the clock deterministically (used by `BindPreconditionServiceTests` to test quote expiry)
- **Global usings**: `global using NUnit.Framework;` lives in each project's `GlobalUsings.cs`, so test files do **not** need to add `using NUnit.Framework;`
- **Total dev-tier tests**: **360** (261 unit/service + 99 HTTP-endpoint/UI integration). Run `dotnet test` to see the current tally. (The QA tier adds 91 Playwright tests — see [§9](#9-two-tier-coverage-map--reduction-review).)

## 2. Run the tests

### Run the full dev-tier suite

```powershell
dotnet test .\InsuranceIntegration.sln
```

### Run only one test project

```powershell
# Unit + service/persistence tests
dotnet test .\tests\dev\InsuranceIntegration.Api.Tests\InsuranceIntegration.Api.Tests.csproj

# HTTP-endpoint + Blazor-UI integration tests
dotnet test .\tests\dev\InsuranceIntegration.Api.IntegrationTests\InsuranceIntegration.Api.IntegrationTests.csproj
```

### Run a single fixture or test

NUnit names match the full type name. Use the `FullyQualifiedName` filter:

```powershell
# Single class
dotnet test --filter "FullyQualifiedName~RiskFlowServiceTests"

# Single test
dotnet test --filter "FullyQualifiedName~RiskFlowServiceTests.Process_AutoClearsWhenEligibilityRulesAreSatisfied"

# Whole namespace
dotnet test --filter "FullyQualifiedName~InsuranceIntegration.Api.Tests.Mappers"
```

### Filter by category

The integration tests carry NUnit categories so you can run focused subsets:

```powershell
$proj = ".\tests\dev\InsuranceIntegration.Api.IntegrationTests\InsuranceIntegration.Api.IntegrationTests.csproj"

# All HTTP-endpoint tests (64)
dotnet test $proj --filter "Category=Api"

# All Blazor-UI tests (35)
dotnet test $proj --filter "Category=Ui"

# Fast sanity subset - health probe + dashboard render (5)
dotnet test $proj --filter "Category=Smoke"
```

Combine with OR (`|`) or exclude with `!=`, e.g. `--filter "Category=Api|Category=Ui"` or `--filter "Category!=Smoke"`.

### Produce a coverage report

```powershell
dotnet test .\tests\dev\InsuranceIntegration.Api.Tests\InsuranceIntegration.Api.Tests.csproj `
  --collect:"XPlat Code Coverage"
```

The Cobertura XML file is written under `TestResults/<guid>/coverage.cobertura.xml` (gitignored in `../.gitignore`).

### Verbose output for debugging

```powershell
dotnet test --logger "console;verbosity=detailed"
```

## 3. Test project layout

```text
tests/dev/InsuranceIntegration.Api.Tests/
  Clearance/
    EfCoreSubmissionRegistryTests.cs        # in-memory SQLite + EF Core
    SubmissionClearanceServiceTests.cs
  Correlation/                              # CorrelationContext scoping
  Flows/
    BillingFlowServiceTests.cs
    BillingFlowInstallmentScheduleTests.cs
    BindPreconditionServiceTests.cs         # FakeTimeProvider quote-expiry tests
    ClaimFlowServiceTests.cs
    ClaimFlowIndemnityTests.cs              # deductible / limit indemnity math
    ComplianceFlowServiceTests.cs
    RiskFlowServiceTests.cs
    RiskFlowTransactionTypeTests.cs
    RiskFlowCoverageTests.cs
    TestRiskRequestFactory.cs               # shared canonical request builder
  Ingest/
    EfCoreIdempotencyStoreTests.cs
    IdempotencyDispatchTests.cs
    IngestDispatcherTests.cs
    RiskIngestHandlerTests.cs
    StubIngestHandler.cs                    # test double
  Mappers/Risks/
    BindPointRiskMapperTests.cs
    ContosoRiskMapperTests.cs
    QuoteForgeRiskMapperTests.cs
    RiskIngestMapperTests.cs
    StubSourceRiskMapper.cs                 # test double
  Matching/                                 # Levenshtein calculator
  Orchestration/
    RiskSubmissionOrchestratorTests.cs      # flow + relational upserts + outbox enqueue
  Outbox/
    OutboxDispatcherTests.cs                # in-memory SQLite + EF Core
    OutboxWriterTests.cs
  Policies/
    PolicyAdjustmentServiceTests.cs         # pure cancellation/endorsement math
    PolicyLifecycleServiceTests.cs          # cancel/endorse end-to-end through router + events
    PolicyRenewalServiceTests.cs            # loss-ratio bands + lineage assertions
    EndorsementSectionOperationsTests.cs    # section/subcover add-remove operations
  Pricing/
    RatingServiceTests.cs
  Products/
    ProductCatalogTests.cs                  # product-code lookups + case-insensitive resolve
  Risks/
    RiskProfileTests.cs                     # risk-profile derivation rules
  Snapshots/
    PolicySnapshotProjectorTests.cs         # pure projector merge rules
    SnapshotPipelineTests.cs                # 3-event end-to-end + DomainEvents assertions
    SnapshotRebuildServiceTests.cs          # replay events -> rebuilt snapshot matches live
  GlobalUsings.cs
  InsuranceIntegration.Api.Tests.csproj
```

Folders mirror the production layout under `src/InsuranceIntegration.Api/Services` and `src/InsuranceIntegration.Api/Mappers`. Keep them aligned when adding new tests.

The HTTP-endpoint and Blazor-UI tests live in a second project:

```text
tests/dev/InsuranceIntegration.Api.IntegrationTests/
  Api/                                      # HTTP-endpoint tests (WebApplicationFactory<Program>)
    HealthEndpointsTests.cs
    SchemaEndpointsTests.cs
    SourceSystemEndpointsTests.cs
    ProductEndpointsTests.cs                # product catalog + rating
    QuoteReadEndpointsTests.cs
    PolicyReadEndpointsTests.cs
    PolicyLifecycleEndpointsTests.cs        # cancel/endorse/renew/reinstate/lapse/non-renew
    BillingEndpointsTests.cs
    ClaimEndpointsTests.cs
    RiskEndpointsTests.cs
    IngestEndpointsTests.cs                 # ingest + idempotent replay
    SecurityEndpointsTests.cs               # X-Api-Key accept/reject (401/200)
    PipelineEndpointsTests.cs               # correlation-id echo + /database 404 gate
    MultiSourceIngestEndpointsTests.cs      # billing/claim/compliance ingest routing
    DevelopmentDataSeederTests.cs           # seeder idempotency (re-seed -> same counts)
  Builders/
    CanonicalRiskRequestBuilder.cs
    QuoteForgeEnvelopeBuilder.cs
    BillingScheduleBuilder.cs
    ExpectedRatingResult.cs                 # rating-math oracle (expected-results builder)
  Infrastructure/
    InsuranceApiFactory.cs                  # WebApplicationFactory<Program> + per-fixture in-memory SQLite
    ApiTestBase.cs                          # Factory/Client + GetAsync/PostAsync helpers
    SeededApiTestBase.cs                    # seeds development data once per fixture
    HttpJsonExtensions.cs                   # ReadAsAsync<T> / ReadAsJsonAsync
    HttpResponseAssertions.cs               # ShouldHaveStatus / ShouldReturnJsonAsync / ShouldReturnAsync<T>
  Ui/                                       # Blazor component tests (bUnit)
    UiPageTestBase.cs                       # per-test BunitContext + Render<TPage>(stub[, params]) + RegisterService<T>
    UiGatewayStub.cs                        # stub IUiGateway returning canned data (lists, details, ingest, tables)
    UiTestData.cs                           # quote/policy/event + detail/table/source-system/receipt factories
    UiAssertions.cs                         # ShouldShowEmptyState / ShouldLinkTo bUnit assertions
    DashboardPageTests.cs
    QuotesPageTests.cs
    PoliciesPageTests.cs
    EventsPageTests.cs
    PolicyDetailPageTests.cs                # not-found / snapshot / quote link / event flow
    QuoteDetailPageTests.cs                 # not-found / snapshot / bind-rejection / policy link
    IngestPageTests.cs                      # template list + prefill / receipt / invalid-JSON guard
    DatabasePageTests.cs                    # disabled gate / table list / rows / no-data
  GlobalUsings.cs
  InsuranceIntegration.Api.IntegrationTests.csproj
```

## 4. Conventions

- One test class per system-under-test, marked `public sealed class`.
- Test method names describe behavior: `Method_ExpectedBehavior_WhenCondition`, e.g. `Process_AutoClearsWhenEligibilityRulesAreSatisfied` (`tests/dev/InsuranceIntegration.Api.Tests/Flows/RiskFlowServiceTests.cs:30`).
- Use `[Test]` attributes; no fixture-level setup attributes are used - prefer constructor initialization + `IDisposable.Dispose` for teardown, e.g. `tests/dev/InsuranceIntegration.Api.Tests/Outbox/OutboxDispatcherTests.cs:10-27`.
- Use `Assert.That(actual, Is.EqualTo(expected))` NUnit constraint-model assertions (not classic `Assert.AreEqual`).
- Shared request builders live alongside the tests that use them (e.g. `TestRiskRequestFactory` in `tests/dev/InsuranceIntegration.Api.Tests/Flows/TestRiskRequestFactory.cs`). Favor named optional parameters so each test tweaks only what it cares about.
- Stubs and fakes sit next to the tests (e.g. `StubIngestHandler.cs`, `StubSourceRiskMapper.cs`). No mocking framework is in use.

## 5. Adding tests

Pick the pattern that matches what you are testing.

### 5.1 Pure service with simple dependencies

Model after `tests/dev/InsuranceIntegration.Api.Tests/Pricing/RatingServiceTests.cs`. Instantiate the service directly with its real collaborators when they are trivial:

```csharp
var service = new RatingService(new ProductCatalog());
var result = service.Rate("COMMERCIAL_PROPERTY", 10_000m, "USD");

Assert.That(result.TechnicalPremium, Is.EqualTo(750m));
```

### 5.2 Canonical flow test using the shared factory

Use `TestRiskRequestFactory.Create(...)` to build a `CanonicalRiskRequest` with deterministic defaults, then override only the fields your scenario needs - see `tests/dev/InsuranceIntegration.Api.Tests/Flows/RiskFlowServiceTests.cs:17-27`.

```csharp
var service = CreateService();
var request = TestRiskRequestFactory.Create(
    brokerPremium: 1200m,
    technicalPremium: 900m,
    annualizedPremium: 800m);

var result = service.Process(request);

Assert.That(result.BasePremium, Is.EqualTo(1200m));
Assert.That(result.AdjustedPremium, Is.GreaterThan(result.BasePremium));
```

### 5.3 Source mapper test

Model after `tests/dev/InsuranceIntegration.Api.Tests/Mappers/Risks/QuoteForgeRiskMapperTests.cs`:

```csharp
var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 8, 30, 0, TimeSpan.Zero));
var mapper = new QuoteForgeRiskMapper(time);
var request = new SourceIngestRequest
{
    SourceSystem = "QUOTEFORGE",
    MessageType = "QuoteRequest",
    Payload = JsonSerializer.SerializeToElement(new { /* source payload */ })
};

var canonical = mapper.Map(request);
```

- Always build `Payload` with `JsonSerializer.SerializeToElement(...)` so you exercise the same deserialization path the production code uses.
- Pair every new mapper with a `CanMap` test and a `Map` test, and remember to register it in `src/InsuranceIntegration.Api/Configuration/ServiceRegistration.cs`.

### 5.4 Persistence test with in-memory SQLite

Use this pattern for any test that needs the real `IntegrationDbContext`. Based on `tests/dev/InsuranceIntegration.Api.Tests/Outbox/OutboxDispatcherTests.cs` and `tests/dev/InsuranceIntegration.Api.Tests/Clearance/EfCoreSubmissionRegistryTests.cs`:

```csharp
public sealed class MyPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IntegrationDbContext> _options;

    public MyPersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new IntegrationDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Test]
    public void Scenario()
    {
        using var context = new IntegrationDbContext(_options);
        // arrange, act, assert
    }

    public void Dispose() => _connection.Dispose();
}
```

Key details:

- Hold the `SqliteConnection` open for the lifetime of the fixture - SQLite throws away the in-memory database when the last connection closes.
- Each `[Test]` creates its own short-lived `IntegrationDbContext` against the shared options.
- Use `context.Database.EnsureCreated()` rather than migrations to keep schema setup per-fixture fast.

### 5.5 Ingest dispatcher / idempotency test

Follow `tests/dev/InsuranceIntegration.Api.Tests/Ingest/IdempotencyDispatchTests.cs`. Create a custom `IIngestHandler` implementation to assert on invocation counts, and pair the dispatcher with `InMemoryIdempotencyStore` to avoid EF Core setup when you only care about dispatch semantics.

### 5.6 Adding a test double

Stubs live in the same folder as the tests that need them. Keep the file `internal`-scoped to the test project, e.g. `tests/dev/InsuranceIntegration.Api.Tests/Ingest/StubIngestHandler.cs`. Prefer simple, readable stubs over reflection-heavy mock frameworks.

### 5.7 HTTP-endpoint integration test

Lives in `tests/dev/InsuranceIntegration.Api.IntegrationTests/Api`. Derive from `ApiTestBase` (empty database) or `SeededApiTestBase` (development data seeded once per fixture); both expose `GetAsync`/`PostAsync` helpers over an in-process `HttpClient`, and `HttpResponseAssertions` gives fluent status/JSON checks. Model after `ProductEndpointsTests`:

```csharp
public sealed class ProductEndpointsTests : ApiTestBase
{
    [Test]
    public async Task ListProducts_ReturnsTheCatalog()
    {
        using var response = await GetAsync("/api/v1/products");

        var products = await response.ShouldReturnAsync<List<ProductDefinition>>();
        Assert.That(products, Is.Not.Empty);
    }
}
```

- Each fixture gets its own isolated in-memory SQLite database (see `Infrastructure/InsuranceApiFactory.cs`), so tests never share state.
- Assert error paths with `response.ShouldHaveStatus(HttpStatusCode.NotFound)` (or `BadRequest`, etc.).
- For API-key-gated writes, construct the base with `: base("integration-test-key")` and send the `X-Api-Key` header on an `HttpRequestMessage` (see `Api/SecurityEndpointsTests.cs`).
- Build request payloads with the fluent builders in `Builders/`, and check rating math against the `ExpectedRatingResult` oracle rather than hard-coding numbers.

### 5.8 Blazor-UI component test (bUnit)

Lives in `tests/dev/InsuranceIntegration.Api.IntegrationTests/Ui`. Derive the fixture from `UiPageTestBase` and render a page against `UiGatewayStub` (a hand-written `IUiGateway` stub) via the inherited `Render<TPage>(stub)` helper. Model after `QuotesPageTests`:

```csharp
public sealed class QuotesPageTests : UiPageTestBase
{
    [Test]
    public void Quotes_RendersARowPerQuote()
    {
        var stub = new UiGatewayStub { Quotes = [UiTestData.Quote()] };

        var cut = Render<Quotes>(stub);

        Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(1));
    }
}
```

- `UiPageTestBase` creates a **fresh `BunitContext` per test** (loose JSInterop) in `[SetUp]` and disposes it in `[TearDown]` - NUnit reuses one fixture instance per class, so sharing a context would leak service registrations between tests. It also declares the `[Category("Ui")]` inherited by every page fixture.
- Reuse the shared bUnit assertions in `UiAssertions`: `cut.ShouldShowEmptyState("No quotes yet.")` (empty-state copy + no data table) and `anchor.ShouldLinkTo(href, text)`.
- The `Events` page name collides with the `InsuranceIntegration.Api.Events` namespace - alias it: `using EventsPage = InsuranceIntegration.Api.Components.Pages.Events;`.
- Drive `<select>` filters with `element.Change(value)` and assert the stub captured the forwarded argument (see `Ui/EventsPageTests.cs`).
- For **detail pages** that take a route parameter, use the parameterized overload: `Render<PolicyDetail>(stub, p => p.Add(x => x.PolicyReference, "POL-PROP-01"))` (see `Ui/PolicyDetailPageTests.cs`).
- For a page that injects a service besides `IUiGateway` (e.g. `Database` injects `DatabaseBrowserGate`), register it first with `RegisterService(new DatabaseBrowserGate(new DatabaseBrowserOptions { Enabled = true }, isDevelopmentEnvironment: false))` (see `Ui/DatabasePageTests.cs`).
- A `<textarea @bind>` exposes its content via the **`value` attribute**, not `TextContent` - read it with `element.GetAttribute("value")` (see `Ui/IngestPageTests.cs`).

## 6. Matching tests to production code

Use this table when debugging a failure or extending a feature:

| Failing / changed area | Start here |
|---|---|
| Endpoint routing or wiring | `src/InsuranceIntegration.Api/Endpoints`, `src/InsuranceIntegration.Api/Program.cs` |
| Dependency registration | `src/InsuranceIntegration.Api/Configuration/ServiceRegistration.cs` |
| Risk flow behavior | `tests/dev/InsuranceIntegration.Api.Tests/Flows/RiskFlowServiceTests.cs`, `.../RiskFlowTransactionTypeTests.cs`, `.../RiskFlowCoverageTests.cs` |
| Bind preconditions (expired / wrong-status / already-bound quote) | `tests/dev/InsuranceIntegration.Api.Tests/Flows/BindPreconditionServiceTests.cs` |
| Source mappers | `tests/dev/InsuranceIntegration.Api.Tests/Mappers/Risks/*` |
| Ingest dispatch + idempotency | `tests/dev/InsuranceIntegration.Api.Tests/Ingest/*` |
| Risk orchestration (relational write model + outbox enqueue) | `tests/dev/InsuranceIntegration.Api.Tests/Orchestration/RiskSubmissionOrchestratorTests.cs` |
| Clearance / fuzzy matching | `tests/dev/InsuranceIntegration.Api.Tests/Clearance/*`, `.../Matching/*` |
| Outbox background dispatch | `tests/dev/InsuranceIntegration.Api.Tests/Outbox/*` |
| Cancellation / endorsement math (pure) | `tests/dev/InsuranceIntegration.Api.Tests/Policies/PolicyAdjustmentServiceTests.cs` |
| Cancellation / endorsement end-to-end (snapshot + DomainEvents) | `tests/dev/InsuranceIntegration.Api.Tests/Policies/PolicyLifecycleServiceTests.cs` |
| Renewal pricing + lineage | `tests/dev/InsuranceIntegration.Api.Tests/Policies/PolicyRenewalServiceTests.cs` |
| Rating / catalog math | `tests/dev/InsuranceIntegration.Api.Tests/Pricing/RatingServiceTests.cs` |
| Correlation ID scoping | `tests/dev/InsuranceIntegration.Api.Tests/Correlation/*` |
| Policy / Quote snapshot projection | `tests/dev/InsuranceIntegration.Api.Tests/Snapshots/PolicySnapshotProjectorTests.cs`, `.../SnapshotPipelineTests.cs` |
| Snapshot rebuild from DomainEvents | `tests/dev/InsuranceIntegration.Api.Tests/Snapshots/SnapshotRebuildServiceTests.cs` |
| DomainEvents row writes / idempotent replay | `tests/dev/InsuranceIntegration.Api.Tests/Snapshots/SnapshotPipelineTests.cs` (asserts event log + history) |
| HTTP endpoint status codes / JSON bodies | `tests/dev/InsuranceIntegration.Api.IntegrationTests/Api/*EndpointsTests.cs` |
| API-key enforcement at the HTTP layer | `tests/dev/InsuranceIntegration.Api.IntegrationTests/Api/SecurityEndpointsTests.cs` |
| HTTP middleware (correlation-id echo, /database 404 gate) | `tests/dev/InsuranceIntegration.Api.IntegrationTests/Api/PipelineEndpointsTests.cs` |
| Multi-source ingest routing (billing/claim/compliance) | `tests/dev/InsuranceIntegration.Api.IntegrationTests/Api/MultiSourceIngestEndpointsTests.cs` |
| Development data seeder idempotency | `tests/dev/InsuranceIntegration.Api.IntegrationTests/Api/DevelopmentDataSeederTests.cs` |
| Product catalog lookups / rating defaults | `tests/dev/InsuranceIntegration.Api.Tests/Products/ProductCatalogTests.cs` |
| Blazor page rendering / pager / filters | `tests/dev/InsuranceIntegration.Api.IntegrationTests/Ui/*PageTests.cs` |

## 7. Manual end-to-end testing

Automated tests cover services in isolation. Use these scripted recipes to exercise the running API. Start the app first (see `docs/guides/USAGE.md`) and substitute your actual port.

For richer per-endpoint scenarios with mandatory-field tables and business-rule annotations, see `docs/reference/API_EXAMPLES.md`. Reusable payloads live in `docs/reference/examples/` and can be POSTed via `Invoke-RestMethod -InFile`.

### 7.1 Smoke test

```powershell
Invoke-RestMethod http://localhost:5000/health
Invoke-RestMethod http://localhost:5000/api/v1/source-systems | Select-Object -First 3
Invoke-RestMethod http://localhost:5000/api/v1/products
```

Expect HTTP 200 on each, no exceptions.

### 7.2 Risk submission via generic ingest (Contoso)

```powershell
$body = @{
  id = "evt-0001"
  source = "CONTOSO_UW"
  type = "RiskSubmission"
  schemaVersion = "1.0"
  occurredAtUtc = (Get-Date).ToUniversalTime().ToString("o")
  correlationId = [guid]::NewGuid().ToString()
  data = @{
    quoteId = "Q-100045"
    insuredName = "Northwind Storage Ltd"
    trade = "CommercialProperty"
    estimatedPremium = 12500.00
  }
} | ConvertTo-Json -Depth 6

$response = Invoke-RestMethod `
  -Uri http://localhost:5000/api/v1/ingest `
  -Method Post `
  -ContentType application/json `
  -Body $body

$response.processedBy    # RiskIngestHandler
$response.outcome.finalStatus
```

### 7.3 Idempotency verification

Re-send the identical envelope and confirm the handler does not run twice:

```powershell
$again = Invoke-RestMethod `
  -Uri http://localhost:5000/api/v1/ingest `
  -Method Post `
  -ContentType application/json `
  -Body $body

# Same envelopeId + source → same result instance returned by the dispatcher
$response.outcome.entityId -eq $again.outcome.entityId
```

Expect `True`. Under the hood, `EfCoreIdempotencyStore` returns the stored `IngestReceipt` - this matches the behavior asserted by `tests/dev/InsuranceIntegration.Api.Tests/Ingest/IdempotencyDispatchTests.cs`. You can also fetch the persisted receipt directly: `GET /api/v1/ingest/CONTOSO_UW/<envelopeId>` returns 200 with the same shape, or 404 if no entry exists for that key.

### 7.4 Canonical risk submission

Copy the canonical JSON from `../README.md:85-203` into `canonical-risk.json`, then:

```powershell
Invoke-RestMethod `
  -Uri http://localhost:5000/api/v1/risks `
  -Method Post `
  -ContentType application/json `
  -InFile .\canonical-risk.json
```

Inspect `clearanceDecision`, `quoteStatus`, `policyStatus`, and `finalStatus` on the response.

### 7.5 Policy lifecycle (cancel / endorse / renew)

These endpoints all require the policy to already exist in `PolicySnapshots` - run §7.2 (Contoso) and a BindPoint envelope first to seed `POL-7781`. Each endpoint returns a `PolicyLifecycleResult` (cancel / endorse) or a `RenewalResult` and writes a row to `DomainEvents` in the same EF transaction as the snapshot mutation.

#### 7.5.1 Cancellation

```powershell
$cancel = @{
  policyReference = "POL-7781"
  annualPremium = 12000
  inceptionDate = "2026-01-01"
  expiryDate = "2026-12-31"
  cancellationDate = "2026-07-01"
  basis = "ProRata"
  shortRatePenaltyPercent = 0.10
  minimumRetainedPremium = 0
} | ConvertTo-Json

$result = Invoke-RestMethod `
  -Uri http://localhost:5000/api/v1/policies/cancellations `
  -Method Post `
  -ContentType application/json `
  -Body $cancel

$result.policyStatus              # Cancelled
$result.cancellation.returnPremium # 6032.97 for the sample above
$result.domainEventType            # PolicyCancelled
```

Expected math: `earnedPremium=5967.03`, `unearnedPremium=6032.97`, `returnPremium=6032.97` (see §7.9 in `docs/guides/USAGE.md`). The same values are asserted by `PolicyAdjustmentServiceTests`; `PolicyLifecycleServiceTests` further asserts the snapshot transition and the `PolicyCancelled` domain event row.

#### 7.5.2 Endorsement

```powershell
$endorse = @{
  policyReference = "POL-7781"
  currentAnnualPremium = 12000
  newAnnualPremium = 13500
  inceptionDate = "2026-01-01"
  expiryDate = "2026-12-31"
  effectiveDate = "2026-07-01"
} | ConvertTo-Json

$result = Invoke-RestMethod `
  -Uri http://localhost:5000/api/v1/policies/endorsements `
  -Method Post `
  -ContentType application/json `
  -Body $endorse

$result.policyStatus                      # Endorsed
$result.endorsement.proRataAdjustment     # 754.12 for the sample above
```

#### 7.5.3 Renewal

```powershell
$renew = @{
  policyReference = "POL-7781"
  newQuoteReference = "QT-RENEWAL-7781"
  newInceptionDate = "2027-01-01"
  newExpiryDate = "2027-12-31"
  priorAnnualPremium = 10000
  priorClaimsPaid = 4500
  revenueDeltaPercent = 0.10
  overrideLoadPercent = $null
} | ConvertTo-Json

$result = Invoke-RestMethod `
  -Uri http://localhost:5000/api/v1/policies/renewals `
  -Method Post `
  -ContentType application/json `
  -Body $renew

$result.lossRatioBand                # Standard (45% loss ratio)
$result.renewalPremium               # 10500 (5% exposure load only)
```

Then confirm the prior policy is now Renewed and a new QuoteSnapshot exists:

```powershell
(Invoke-RestMethod http://localhost:5000/api/v1/policies/POL-7781).lifecycle.currentPhase     # Renewed
(Invoke-RestMethod http://localhost:5000/api/v1/quotes/QT-RENEWAL-7781).priorPolicyReference  # POL-7781
```

Loss-ratio bands and pricing math are exhaustively asserted by `PolicyRenewalServiceTests` (parameterised over `Excellent`/`Standard`/`Loaded`/`HeavilyLoaded`/`Distressed`).

#### 7.5.4 Snapshot rebuild from DomainEvents

After running cancel / endorse / renew, replay the policy's events through the projector and compare the rebuilt snapshot against the live one:

```powershell
$rebuilt = Invoke-RestMethod -Method Post http://localhost:5000/api/v1/snapshots/policies/POL-7781/rebuild

$rebuilt.eventsApplied                       # number of policy events replayed
$rebuilt.snapshot.lifecycle.policyStatus     # should match the live snapshot
```

The rebuild is asserted to reproduce the live state by `SnapshotRebuildServiceTests`.

### 7.6 Outbox dispatch

Trigger any flow that writes to the outbox (e.g. a risk submission), then watch the server log. After up to ~2 seconds you should see entries shaped like:

```
Dispatching outbox event <EventType> for <AggregateType> <AggregateId> (EventId=..., CorrelationId=...).
```

You can also confirm rows move from `DispatchedAtUtc IS NULL` to a set timestamp by inspecting the SQLite file with any SQLite tool:

```powershell
sqlite3 .\integration.db "SELECT EventId, EventType, DispatchedAtUtc FROM OutboxMessages ORDER BY OccurredAtUtc DESC LIMIT 10;"
```

The automated equivalent lives in `tests/dev/InsuranceIntegration.Api.Tests/Outbox/OutboxDispatcherTests.cs`.

### 7.7 Correlation tracing

```powershell
$corr = [guid]::NewGuid().ToString()
$response = Invoke-WebRequest `
  -Uri http://localhost:5000/health `
  -Headers @{ "X-Correlation-Id" = $corr }

$response.Headers["X-Correlation-Id"]   # echoes $corr
```

The same value is attached to the request's log scope on the server side.

## 8. Troubleshooting test failures

- **`SqliteException: SQLite Error 1: 'no such table: ...'`** - the test forgot to call `context.Database.EnsureCreated()`, or the `SqliteConnection` was closed before the test ran. See §5.4 for the correct pattern.
- **`InvalidOperationException: No risk mapper registered for source 'X' and message type 'Y'.`** - a new mapper was added but not registered in `src/InsuranceIntegration.Api/Configuration/ServiceRegistration.cs`. Register it and rerun.
- **Flaky time-sensitive tests** - inject `TimeProvider` via the constructor (as `src/InsuranceIntegration.Api/Services/Outbox/OutboxDispatcher.cs` does). Use `TimeProvider.System` in tests or a controllable fake if you need to pin `now`.
- **`dotnet test` says "No test is available in ..."** - confirm the test class is `public` and methods are annotated with `[Test]`. `internal` classes are not discovered.
- **Coverage file missing** - the `--collect` argument must be quoted exactly as `"XPlat Code Coverage"`; otherwise PowerShell splits it into multiple tokens.

## 9. Two-tier coverage map & reduction review

The suite is deliberately split into a **dev tier** (white-box, in-process, .NET/NUnit under `tests/dev/`) and a **QA tier** (black-box, against the running app, TypeScript/Playwright under `tests/qa/`). This section documents the QA tier and records the **2026 test-reduction review** that checked whether the two tiers overlap enough to safely cut tests.

### 9.1 QA tier (Playwright black-box) — `tests/qa/`

A self-contained Node/TypeScript project; it has its own `package.json`, `tsconfig.json`, `playwright.config.ts`, and README. It exercises the **deployed app** end to end — real HTTP, a real browser, real seeded data — and is the closest automated proxy for a user or downstream integrator.

```text
tests/qa/
  playwright.config.ts          # projects: api, ui-chromium, ui-firefox, ui-webkit, a11y
  test-targets.config.ts        # base URLs (INSURANCE_APP_BASE_URL, default http://localhost:5000)
  src/
    core/api/base-api.client.ts # BaseApiClient (get/post/put/patch/delete/expectOk)
    core/ui/base.page.ts        # BasePage (visit/expectVisible/click/textContents)
    clients/insurance-app/      # InsuranceApiClient — typed wrapper over every endpoint
    pages/insurance-app/        # page objects (nav, home, quotes, quote-detail, policies, ingest, events)
    builders/
      objects/                  # QuoteForgeEnvelopeBuilder (fluent, immutable, unique ids)
      expected/                 # expectedRating — TS port of RatingService (results oracle)
      requests/                 # PagedQueryBuilder (skip/take/page → query string)
    models/api/                 # *.dto.ts response contracts
    fixtures/insurance-app/     # test = base.extend<{ api, nav, pages... }>
    helpers/                    # unique-id, a11y (assertNoSeriousA11yViolations)
  specs/insurance-app/
    api/                        # *.api.spec.ts — health, products, source-systems, quotes,
                                #   policies, ingest, schemas, rating (32 tests)
    ui/                         # *.spec.ts — navigation, dashboard, quotes, policies, ingest
                                #   (18 tests × 3 browsers = 54)
    a11y/                       # *.a11y.spec.ts — axe WCAG 2.0/2.1 A+AA on 5 routes (5 tests)
```

**Total QA-tier tests: 91** (32 API + 54 UI across three browsers + 5 a11y).

Run it from `tests/qa/` (the `webServer` block auto-starts the API on `http://localhost:5000` against an isolated SQLite DB in `tests/qa/.tmp/`, so QA runs never touch the dev database):

```powershell
Set-Location .\tests\qa
npm install            # first time only
npx playwright install # first time only — browser binaries

npm test               # full suite (api + ui×3 + a11y)
npm run test:api       # API project only
npm run test:ui        # UI projects (all three browsers)
npm run test:a11y      # accessibility project only
npm run test:smoke     # @smoke-tagged subset
npm run report         # open the last HTML report
```

QA-tier conventions: one `BaseApiClient`/`BasePage` per concern; fluent immutable builders in three tiers (**objects**, **expected results**, **requests**); `*.dto.ts` response contracts; `data-test` as the Playwright `testIdAttribute`; `@smoke`/`@critical` tags; resilient assertions (floors such as `>= 28` quotes rather than exact counts, unique envelope ids, superset catalog checks) so the black-box specs tolerate seed growth. Full details live in `tests/qa/README.md`.

> The a11y project earns its keep: it caught a genuine WCAG AA contrast defect in the app's status badges (`wwwroot/app.css`), which was fixed rather than suppressed — the QA value loop working as intended.

### 9.2 Coverage map — who proves what

| Surface | Dev tier (white-box) | QA tier (black-box) |
|---|---|---|
| Rating math | `RatingServiceTests` (premium **decomposition**: min-premium applied, load amount, exception type) | `rating.api.spec.ts` (endpoint premium vs independent oracle) |
| Product catalog | `ProductCatalogTests` (case-insensitive lookup, null-for-unknown) | `products.api.spec.ts` (catalog list contract) |
| Quote/policy read | `QuoteReadEndpointsTests`, `PolicyReadEndpointsTests` (in-process JSON shape) | `quotes/policies.api.spec.ts` (live paging, detail snapshot) |
| Ingest | unit dispatch + `IngestEndpointsTests` (idempotent replay) | `ingest.api.spec.ts` (live envelope → receipt → projection) |
| Pages (render branches) | bUnit `*PageTests` (**empty states, not-found, null props, rejection notes** with controlled data) | `ui/*.spec.ts` (real browser, real data, **3 engines**, navigation) |
| Accessibility | — | `a11y/surfaces.a11y.spec.ts` (axe WCAG) |
| Cross-browser | — | Chromium + Firefox + WebKit |

### 9.3 Reduction review (2026) — conclusion: keep both tiers

The review asked whether adding the QA tier made dev-tier tests redundant enough to delete. Candidate-by-candidate verification (reading each test, not just its name) found the apparent overlap is mostly **complementary coverage**, so no wholesale deletion was made:

- **bUnit UI tests are *not* redundant with Playwright UI specs.** The bUnit fixtures drive a stubbed `IUiGateway`, so they cover render branches the black-box specs cannot reach against a seeded database — empty states (no quotes/policies/events), not-found alerts, bind-rejection notes, and null/omitted-field handling. Playwright covers the happy paths end-to-end across three browsers plus a11y. Deleting the bUnit suite would drop the edge-case branch coverage.
- **The six `PolicyLifecycleEndpointsTests` "unknown policy → 404" cases each guard a *distinct* endpoint's wiring** (cancel/endorse/renew/reinstate/lapse/non-renew). They look like one repeated assertion but each protects a different route against silently returning 200 for a missing policy.
- **`RatingServiceTests` and `ProductCatalogTests` assert internals the HTTP/QA tests can't see** (premium decomposition, exception types, case-insensitive resolution). The QA rating oracle validates the *endpoint result*; the unit tests pin the *math breakdown*.

**Net safe deletions: 0.** The maintenance win came from *architecture*, not deletion: the two tiers are cleanly separated (`tests/dev` vs `tests/qa`), each with a single responsibility, so a change touches exactly one tier. Optional future consolidations (require sign-off, each trades coverage for fewer tests): collapsing the bUnit happy-path render assertions now that Playwright covers them (loses fast in-process render pinning), or running rendering-insensitive UI specs on a single browser instead of three (loses cross-engine coverage). Neither was applied, because the conservative, coverage-preserving choice is to keep them.

