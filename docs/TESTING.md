# Testing Guide

How to run, extend, and manually exercise the test suite for **InsuranceIntegration.Api**.

## 1. Test stack

- **Framework**: [NUnit 4.4](https://docs.nunit.org/) (see `../tests/InsuranceIntegration.Api.Tests/InsuranceIntegration.Api.Tests.csproj`)
- **Test runner**: `dotnet test` via `Microsoft.NET.Test.Sdk` + `NUnit3TestAdapter`
- **Coverage**: `coverlet.collector` is available for producing Cobertura reports
- **Persistence in tests**: EF Core 10 + `Microsoft.Data.Sqlite` in-memory (`DataSource=:memory:`) — no external DB required
- **Global usings**: `global using NUnit.Framework;` lives in `../tests/InsuranceIntegration.Api.Tests/GlobalUsings.cs`, so test files do **not** need to add `using NUnit.Framework;`

## 2. Run the tests

### Run the full suite

```powershell
dotnet test .\InsuranceIntegration.sln
```

### Run only the API test project

```powershell
dotnet test .\tests\InsuranceIntegration.Api.Tests\InsuranceIntegration.Api.Tests.csproj
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

### Produce a coverage report

```powershell
dotnet test .\tests\InsuranceIntegration.Api.Tests\InsuranceIntegration.Api.Tests.csproj `
  --collect:"XPlat Code Coverage"
```

The Cobertura XML file is written under `TestResults/<guid>/coverage.cobertura.xml` (gitignored in `../.gitignore`).

### Verbose output for debugging

```powershell
dotnet test --logger "console;verbosity=detailed"
```

## 3. Test project layout

```text
tests/InsuranceIntegration.Api.Tests/
  Clearance/
    EfCoreSubmissionRegistryTests.cs        # in-memory SQLite + EF Core
    SubmissionClearanceServiceTests.cs
  Correlation/                              # CorrelationContext scoping
  Flows/
    BillingFlowServiceTests.cs
    ClaimFlowServiceTests.cs
    ComplianceFlowServiceTests.cs
    RiskFlowServiceTests.cs
    RiskFlowTransactionTypeTests.cs
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
  Outbox/
    OutboxDispatcherTests.cs                # in-memory SQLite + EF Core
    OutboxWriterTests.cs
  Policies/
    PolicyAdjustmentServiceTests.cs
  Pricing/
    RatingServiceTests.cs
  Snapshots/
    PolicySnapshotProjectorTests.cs         # pure projector merge rules
    SnapshotPipelineTests.cs                # 3-event end-to-end through dispatcher + EF
  GlobalUsings.cs
  InsuranceIntegration.Api.Tests.csproj
```

Folders mirror the production layout under `../src/InsuranceIntegration.Api/Services` and `../src/InsuranceIntegration.Api/Mappers`. Keep them aligned when adding new tests.

## 4. Conventions

- One test class per system-under-test, marked `public sealed class`.
- Test method names describe behavior: `Method_ExpectedBehavior_WhenCondition`, e.g. `Process_AutoClearsWhenEligibilityRulesAreSatisfied` (`../tests/InsuranceIntegration.Api.Tests/Flows/RiskFlowServiceTests.cs:30`).
- Use `[Test]` attributes; no fixture-level setup attributes are used — prefer constructor initialization + `IDisposable.Dispose` for teardown, e.g. `../tests/InsuranceIntegration.Api.Tests/Outbox/OutboxDispatcherTests.cs:10-27`.
- Use `Assert.That(actual, Is.EqualTo(expected))` NUnit constraint-model assertions (not classic `Assert.AreEqual`).
- Shared request builders live alongside the tests that use them (e.g. `TestRiskRequestFactory` in `../tests/InsuranceIntegration.Api.Tests/Flows/TestRiskRequestFactory.cs`). Favor named optional parameters so each test tweaks only what it cares about.
- Stubs and fakes sit next to the tests (e.g. `StubIngestHandler.cs`, `StubSourceRiskMapper.cs`). No mocking framework is in use.

## 5. Adding tests

Pick the pattern that matches what you are testing.

### 5.1 Pure service with simple dependencies

Model after `../tests/InsuranceIntegration.Api.Tests/Pricing/RatingServiceTests.cs`. Instantiate the service directly with its real collaborators when they are trivial:

```csharp
var service = new RatingService(new ProductCatalog());
var result = service.Rate("COMMERCIAL_PROPERTY", 10_000m, "USD");

Assert.That(result.TechnicalPremium, Is.EqualTo(750m));
```

### 5.2 Canonical flow test using the shared factory

Use `TestRiskRequestFactory.Create(...)` to build a `CanonicalRiskRequest` with deterministic defaults, then override only the fields your scenario needs — see `../tests/InsuranceIntegration.Api.Tests/Flows/RiskFlowServiceTests.cs:17-27`.

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

Model after `../tests/InsuranceIntegration.Api.Tests/Mappers/Risks/QuoteForgeRiskMapperTests.cs`:

```csharp
var mapper = new QuoteForgeRiskMapper();
var request = new SourceIngestRequest
{
    SourceSystem = "QUOTEFORGE",
    MessageType = "QuoteRequest",
    Payload = JsonSerializer.SerializeToElement(new { /* source payload */ })
};

var canonical = mapper.Map(request);
```

- Always build `Payload` with `JsonSerializer.SerializeToElement(...)` so you exercise the same deserialization path the production code uses.
- Pair every new mapper with a `CanMap` test and a `Map` test, and remember to register it in `../src/InsuranceIntegration.Api/Configuration/ServiceRegistration.cs`.

### 5.4 Persistence test with in-memory SQLite

Use this pattern for any test that needs the real `IntegrationDbContext`. Based on `../tests/InsuranceIntegration.Api.Tests/Outbox/OutboxDispatcherTests.cs` and `../tests/InsuranceIntegration.Api.Tests/Clearance/EfCoreSubmissionRegistryTests.cs`:

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

- Hold the `SqliteConnection` open for the lifetime of the fixture — SQLite throws away the in-memory database when the last connection closes.
- Each `[Test]` creates its own short-lived `IntegrationDbContext` against the shared options.
- Use `context.Database.EnsureCreated()` rather than migrations to keep schema setup per-fixture fast.

### 5.5 Ingest dispatcher / idempotency test

Follow `../tests/InsuranceIntegration.Api.Tests/Ingest/IdempotencyDispatchTests.cs`. Create a custom `IIngestHandler` implementation to assert on invocation counts, and pair the dispatcher with `InMemoryIdempotencyStore` to avoid EF Core setup when you only care about dispatch semantics.

### 5.6 Adding a test double

Stubs live in the same folder as the tests that need them. Keep the file `internal`-scoped to the test project, e.g. `../tests/InsuranceIntegration.Api.Tests/Ingest/StubIngestHandler.cs`. Prefer simple, readable stubs over reflection-heavy mock frameworks.

## 6. Matching tests to production code

Use this table when debugging a failure or extending a feature:

| Failing / changed area | Start here |
|---|---|
| Endpoint routing or wiring | `../src/InsuranceIntegration.Api/Endpoints`, `../src/InsuranceIntegration.Api/Program.cs` |
| Dependency registration | `../src/InsuranceIntegration.Api/Configuration/ServiceRegistration.cs` |
| Risk flow behavior | `../tests/InsuranceIntegration.Api.Tests/Flows/RiskFlowServiceTests.cs`, `.../RiskFlowTransactionTypeTests.cs` |
| Source mappers | `../tests/InsuranceIntegration.Api.Tests/Mappers/Risks/*` |
| Ingest dispatch + idempotency | `../tests/InsuranceIntegration.Api.Tests/Ingest/*` |
| Clearance / fuzzy matching | `../tests/InsuranceIntegration.Api.Tests/Clearance/*`, `.../Matching/*` |
| Outbox background dispatch | `../tests/InsuranceIntegration.Api.Tests/Outbox/*` |
| Cancellation / endorsement math | `../tests/InsuranceIntegration.Api.Tests/Policies/PolicyAdjustmentServiceTests.cs` |
| Rating / catalog math | `../tests/InsuranceIntegration.Api.Tests/Pricing/RatingServiceTests.cs` |
| Correlation ID scoping | `../tests/InsuranceIntegration.Api.Tests/Correlation/*` |
| Policy / Quote snapshot projection | `../tests/InsuranceIntegration.Api.Tests/Snapshots/*` (projector merge rules + 3-event pipeline) |

## 7. Manual end-to-end testing

Automated tests cover services in isolation. Use these scripted recipes to exercise the running API. Start the app first (see `./USAGE.md`) and substitute your actual port.

For richer per-endpoint scenarios with mandatory-field tables and business-rule annotations, see `./API_EXAMPLES.md`. Reusable payloads live in `./examples/` and can be POSTed via `Invoke-RestMethod -InFile`.

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

Expect `True`. Under the hood, `EfCoreIdempotencyStore` returns the stored `IngestReceipt` — this matches the behavior asserted by `../tests/InsuranceIntegration.Api.Tests/Ingest/IdempotencyDispatchTests.cs`. You can also fetch the persisted receipt directly: `GET /api/v1/ingest/CONTOSO_UW/<envelopeId>` returns 200 with the same shape, or 404 if no entry exists for that key.

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

### 7.5 Policy math

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

Invoke-RestMethod `
  -Uri http://localhost:5000/api/v1/policies/cancellations `
  -Method Post `
  -ContentType application/json `
  -Body $cancel
```

Expected values for the sample above: `earnedPremium=5967.03`, `unearnedPremium=6032.97`, `returnPremium=6032.97` (see §7.9 in `./USAGE.md`). The same values are asserted — within a range — by `PolicyAdjustmentServiceTests`.

### 7.6 Outbox dispatch

Trigger any flow that writes to the outbox (e.g. a risk submission), then watch the server log. After up to ~2 seconds you should see entries shaped like:

```
Dispatching outbox event <EventType> for <AggregateType> <AggregateId> (EventId=..., CorrelationId=...).
```

You can also confirm rows move from `DispatchedAtUtc IS NULL` to a set timestamp by inspecting the SQLite file with any SQLite tool:

```powershell
sqlite3 .\integration.db "SELECT EventId, EventType, DispatchedAtUtc FROM OutboxMessages ORDER BY OccurredAtUtc DESC LIMIT 10;"
```

The automated equivalent lives in `../tests/InsuranceIntegration.Api.Tests/Outbox/OutboxDispatcherTests.cs`.

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

- **`SqliteException: SQLite Error 1: 'no such table: ...'`** — the test forgot to call `context.Database.EnsureCreated()`, or the `SqliteConnection` was closed before the test ran. See §5.4 for the correct pattern.
- **`InvalidOperationException: No risk mapper registered for source 'X' and message type 'Y'.`** — a new mapper was added but not registered in `../src/InsuranceIntegration.Api/Configuration/ServiceRegistration.cs`. Register it and rerun.
- **Flaky time-sensitive tests** — inject `TimeProvider` via the constructor (as `../src/InsuranceIntegration.Api/Services/Outbox/OutboxDispatcher.cs` does). Use `TimeProvider.System` in tests or a controllable fake if you need to pin `now`.
- **`dotnet test` says "No test is available in ..."** — confirm the test class is `public` and methods are annotated with `[Test]`. `internal` classes are not discovered.
- **Coverage file missing** — the `--collect` argument must be quoted exactly as `"XPlat Code Coverage"`; otherwise PowerShell splits it into multiple tokens.
