# Usage Guide

How to build, run, and exercise the **InsuranceIntegration.Api** service.

The service is a headless ASP.NET Core Web API (see `../src/InsuranceIntegration.Api/Program.cs`). All interaction is HTTP/JSON. In `Development` it exposes Swagger UI at `/swagger` for interactive exploration.

## 1. Prerequisites

- **.NET SDK 10.0** (matches `TargetFramework` in `../src/InsuranceIntegration.Api/InsuranceIntegration.Api.csproj`)
- Windows PowerShell or any shell capable of running `dotnet`
- No external database required — SQLite is used by default and created on first run

Verify your SDK:

```powershell
dotnet --list-sdks
```

## 2. Build

```powershell
dotnet build .\InsuranceIntegration.sln
```

## 3. Run

```powershell
dotnet run --project .\src\InsuranceIntegration.Api\InsuranceIntegration.Api.csproj
```

On startup the host logs the listening URL (by default something like `http://localhost:5000`). All subsequent examples assume `http://localhost:5000` — substitute your actual host/port.

The app will:

1. Apply any pending EF Core migrations against the configured SQLite database, or create the schema if no migrations have been applied (`../src/InsuranceIntegration.Api/Program.cs:14-25`).
2. Start the `OutboxDispatcher` background service which polls the `OutboxMessages` table every 2 seconds and marks pending events as dispatched (`../src/InsuranceIntegration.Api/Services/Outbox/OutboxDispatcher.cs`).
3. Wire up the `X-Correlation-Id` middleware (`../src/InsuranceIntegration.Api/Middleware/CorrelationIdMiddleware.cs`).

### Override the listening URL

```powershell
$env:ASPNETCORE_URLS = "http://localhost:5080"
dotnet run --project .\src\InsuranceIntegration.Api\InsuranceIntegration.Api.csproj
```

## 4. Configuration

Configuration is read via `IConfiguration` and applied in `../src/InsuranceIntegration.Api/Configuration/ServiceRegistration.cs`.

### Database connection string

- **Key**: `ConnectionStrings:Integration`
- **Default**: `Data Source=integration.db` (SQLite file created in the working directory)
- **Override examples**:

```powershell
# environment variable
$env:ConnectionStrings__Integration = "Data Source=C:\data\integration.db"

# command-line
dotnet run --project .\src\InsuranceIntegration.Api\InsuranceIntegration.Api.csproj -- --ConnectionStrings:Integration="Data Source=C:\data\integration.db"
```

The context (`IntegrationDbContext`) owns three tables:

| Table | Purpose |
|---|---|
| `KnownSubmissions` | Clearance registry for previously seen submissions |
| `InboxMessages` | Idempotency cache keyed by `(Source, EnvelopeId)` |
| `OutboxMessages` | Events awaiting dispatch by the background worker |

Generated migrations live in `../src/InsuranceIntegration.Api/Persistence/Migrations`.

## 5. Discover the API

### Swagger UI (Development only)

```
http://localhost:5000/swagger
```

### OpenAPI document

```
http://localhost:5000/openapi/v1.json
```

### Machine-readable JSON Schemas for the main contracts

```
GET /api/v1/schemas/ingest/envelope
GET /api/v1/schemas/ingest/risk-request
GET /api/v1/schemas/canonical/risk-request
GET /api/v1/schemas/final/risk-response
```

Source: `../src/InsuranceIntegration.Api/Endpoints/SchemaEndpoints.cs`.

## 6. Cross-cutting concerns

### Correlation IDs

Every request passes through `CorrelationIdMiddleware`. It reads/generates two headers (`../src/InsuranceIntegration.Api/Middleware/CorrelationIdMiddleware.cs`):

- **`X-Correlation-Id`** — If the caller sends a valid GUID it is reused; otherwise a new UUIDv7 is generated. It is echoed back on the response and included in log scopes.
- **`X-Causation-Id`** — Optional GUID. When present it is added to log scopes.

Always pass `X-Correlation-Id` from upstream systems to make tracing possible.

### Idempotency for ingest

`POST /api/v1/ingest` is idempotent per `(Source, Id)`. When the same envelope `Id` is sent again for the same `Source`, the dispatcher returns the previously stored `IngestAcceptedResult` without re-running the handler (`../src/InsuranceIntegration.Api/Services/Ingest/IngestDispatcher.cs:17-41`). This is persisted in `InboxMessages` via `EfCoreIdempotencyStore`.

### Outbox

Canonical flows (risk, claim, billing, compliance) can enqueue events via `IOutboxWriter` into the `OutboxMessages` table. The `OutboxDispatcher` background service polls every 2 seconds, batches up to 50 pending rows, logs a dispatch line per event, and marks them dispatched (`../src/InsuranceIntegration.Api/Services/Outbox/OutboxDispatcher.cs`). Real transport integration is not implemented yet — logs are the current sink.

## 7. Endpoint reference

All endpoints are mapped in `Program.cs` via the route groups under `../src/InsuranceIntegration.Api/Endpoints`.

Summary:

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/health` | Liveness probe |
| `GET` | `/api/v1/source-systems` | Catalog of supported source systems and their example payload shapes |
| `GET` | `/api/v1/products` | Product catalog used by rating and downstream flows |
| `GET` | `/api/v1/products/{productCode}/rating` | On-demand rating calculation for a product |
| `POST` | `/api/v1/ingest` | Generic envelope ingest — dispatched by `(Source, Type)` with idempotency |
| `POST` | `/api/v1/ingest/risks` | Source-shape risk ingest (selects a source-specific mapper) |
| `POST` | `/api/v1/risks` | Canonical risk submission (skips source mapping) |
| `POST` | `/api/v1/policies/cancellations` | Pro-rata or short-rate cancellation calculation |
| `POST` | `/api/v1/policies/endorsements` | Mid-term endorsement delta calculation |
| `GET` | `/api/v1/schemas/ingest/envelope` | JSON schema for `SourceIngestEnvelope` |
| `GET` | `/api/v1/schemas/ingest/risk-request` | JSON schema for `SourceIngestRequest` |
| `GET` | `/api/v1/schemas/canonical/risk-request` | JSON schema for `CanonicalRiskRequest` |
| `GET` | `/api/v1/schemas/final/risk-response` | JSON schema for `FinalRiskResponse` |

---

### 7.1 `GET /health`

Minimal liveness check (`../src/InsuranceIntegration.Api/Endpoints/HealthEndpoints.cs`).

**Request**

```powershell
Invoke-RestMethod http://localhost:5000/health
```

**Response (200 OK)**

```json
{
  "status": "Healthy",
  "service": "InsuranceIntegration.Api",
  "framework": ".NET 10.0.0"
}
```

---

### 7.2 `GET /api/v1/source-systems`

Returns the catalog of supported source systems, each with a small illustrative `examplePayload` that reflects what the source would send inside a `SourceIngestEnvelope.Data` (`../src/InsuranceIntegration.Api/Services/Catalog/SourceSystemCatalogService.cs`).

**Request**

```powershell
Invoke-RestMethod http://localhost:5000/api/v1/source-systems
```

**Response (200 OK)** — abbreviated, 12 entries total:

```json
[
  {
    "systemCode": "CONTOSO_UW",
    "displayName": "Contoso Underwriting Workbench",
    "businessPurpose": "Risk intake and underwriting submissions",
    "messageType": "RiskSubmission",
    "examplePayload": {
      "quoteId": "Q-100045",
      "insuredName": "Northwind Storage Ltd",
      "trade": "CommercialProperty",
      "estimatedPremium": 12500.00
    }
  }
]
```

---

### 7.3 `GET /api/v1/products`

Returns the rating-aware product catalog (`../src/InsuranceIntegration.Api/Services/Products/ProductCatalog.cs`).

**Response (200 OK)**

```json
[
  { "productCode": "COMMERCIAL_PROPERTY", "displayName": "Commercial Property", "family": "Property", "baseRatePerThousandRevenue": 2.50, "minimumPremium": 750, "largeAccountThreshold": 10000000, "largeAccountLoad": 0.05 },
  { "productCode": "LIABILITY",           "displayName": "General Liability",  "family": "Liability", "baseRatePerThousandRevenue": 1.80, "minimumPremium": 500, "largeAccountThreshold": 10000000, "largeAccountLoad": 0.05 },
  { "productCode": "CYBER",               "displayName": "Cyber Liability",    "family": "Cyber",     "baseRatePerThousandRevenue": 4.00, "minimumPremium": 1500, "largeAccountThreshold": 10000000, "largeAccountLoad": 0.05 },
  { "productCode": "MOTOR",               "displayName": "Commercial Motor",   "family": "Auto",      "baseRatePerThousandRevenue": 3.20, "minimumPremium": 900, "largeAccountThreshold": 10000000, "largeAccountLoad": 0.05 }
]
```

---

### 7.4 `GET /api/v1/products/{productCode}/rating`

On-demand rating for a given product and insured revenue. Reasons are populated in order of application (`../src/InsuranceIntegration.Api/Services/Pricing/RatingService.cs`).

**Query parameters**

- `annualRevenue` *(decimal, required)*
- `currencyCode` *(string, optional, defaults to `USD`)*

**Request**

```powershell
Invoke-RestMethod "http://localhost:5000/api/v1/products/COMMERCIAL_PROPERTY/rating?annualRevenue=2200000&currencyCode=USD"
```

**Response (200 OK)**

```json
{
  "productCode": "COMMERCIAL_PROPERTY",
  "baseRate": 2.50,
  "revenueBasedPremium": 5500.00,
  "appliedMinimumPremium": 750,
  "largeAccountLoadAmount": 0,
  "technicalPremium": 5500.00,
  "currencyCode": "USD",
  "ratingReasons": [
    "Revenue-based premium: 5500 (2200 x 2.50)"
  ]
}
```

Unknown product codes yield HTTP `500` with an `InvalidOperationException` message (`Product '<code>' is not defined in the catalog.`).

---

### 7.5 `POST /api/v1/ingest`

The generic ingest endpoint. Dispatches to a handler selected by `(Source, Type)` with idempotency keyed on `(Source, Id)` (`../src/InsuranceIntegration.Api/Endpoints/IngestEndpoints.cs:12-16`, `../src/InsuranceIntegration.Api/Services/Ingest/IngestDispatcher.cs`).

**Envelope shape** (`SourceIngestEnvelope`)

| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | Unique per `source`. Drives idempotency. |
| `source` | string | yes | e.g. `CONTOSO_UW`, `CLAIMFORGE` |
| `type` | string | yes | e.g. `RiskSubmission`, `ClaimNotice` |
| `schemaVersion` | string | yes | Caller-managed versioning tag |
| `occurredAtUtc` | datetime (UTC) | yes | Source event time |
| `correlationId` | string | no | Optional GUID, carried into downstream logs |
| `data` | JSON object | yes | Source-specific payload |

#### Dispatch matrix

| `source` | `type` | Routed to | Payload shape |
|---|---|---|---|
| `CONTOSO_UW` | `RiskSubmission` | `RiskIngestHandler` → `ContosoRiskMapper` | `ContosoRiskSubmissionPayload` |
| `QUOTEFORGE` | `QuoteRequest` | `RiskIngestHandler` → `QuoteForgeRiskMapper` | `QuoteForgeQuoteRequestPayload` |
| `BINDPOINT` | `PolicyBindRequest` | `RiskIngestHandler` → `BindPointRiskMapper` | `BindPointPolicyBindPayload` |
| *any* | `ClaimNotice`, `LossRun` | `ClaimIngestHandler` | `ClaimNoticePayload` |
| *any* | `InstallmentSchedule` | `BillingIngestHandler` | `InstallmentSchedulePayload` |
| *any* | `ComplianceResult`, `FraudAssessment` | `ComplianceIngestHandler` | `ComplianceResultPayload` |

Supported types come from each handler's `SupportedTypes` set, e.g. `../src/InsuranceIntegration.Api/Services/Ingest/RiskIngestHandler.cs:9-14`. Any unmatched `(source, type)` combination causes an `InvalidOperationException` — "No ingest handler registered for source '<source>' and type '<type>'.".

**Request (Contoso risk submission)**

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

Invoke-RestMethod `
  -Uri http://localhost:5000/api/v1/ingest `
  -Method Post `
  -ContentType application/json `
  -Body $body
```

**Response (200 OK)** — outer envelope is `IngestAcceptedResult` with the handler's own output in `result` (for risks this is a `FinalRiskResponse`):

```json
{
  "envelopeId": "evt-0001",
  "source": "CONTOSO_UW",
  "type": "RiskSubmission",
  "handlerName": "RiskIngestHandler",
  "correlationId": "7f3d...",
  "result": {
    "entityId": "…",
    "externalReference": "Q-100045",
    "productCode": "COMMERCIALPROPERTY",
    "sourceSystem": "CONTOSO_UW",
    "transactionType": "Submission",
    "submissionStatus": "…",
    "quoteStatus": "…",
    "policyStatus": "…",
    "clearanceDecision": "…",
    "finalStatus": "…",
    "...": "see FinalRiskResponse for the full field list"
  }
}
```

Resending the same `id` + `source` returns the stored result without re-running the handler.

---

### 7.6 `POST /api/v1/ingest/risks`

Source-shape risk ingest for risk lifecycle only. Bypasses the generic dispatcher and directly invokes `IRiskIngestMapper` + `IRiskFlowService` (`../src/InsuranceIntegration.Api/Endpoints/IngestEndpoints.cs:18-23`).

**Body** (`SourceIngestRequest`)

```json
{
  "sourceSystem": "QUOTEFORGE",
  "messageType": "QuoteRequest",
  "payload": {
    "quoteReference": "QF-2200",
    "insuredName": "Harborline Services",
    "productLine": "LIABILITY",
    "brokerCode": "BRK-044",
    "brokerName": "Harbor Broking",
    "technicalPremium": 8200.00,
    "brokerPremium": 8450.00,
    "currencyCode": "USD",
    "effectiveDate": "2026-05-01",
    "expiryDate": "2027-04-30",
    "underwritingYear": 2026,
    "insuredRevenue": 2200000.00,
    "insuredEmployeeCount": 40,
    "insuredYearsInBusiness": 8
  }
}
```

**Response (200 OK)** — a `FinalRiskResponse` (see §7.8).

Allowed `(sourceSystem, messageType)` pairs match the risk entries in the dispatch matrix above. Unmatched pairs produce `InvalidOperationException` — "No risk mapper registered for source '<sourceSystem>' and message type '<messageType>'." (`../src/InsuranceIntegration.Api/Mappers/Risks/RiskIngestMapper.cs`).

---

### 7.7 `POST /api/v1/risks`

Submits a canonical risk request directly — useful when the caller already owns the platform-neutral shape (`../src/InsuranceIntegration.Api/Endpoints/RiskEndpoints.cs`).

For a complete end-to-end sample body, see `../README.md:85-203`.

**Response (200 OK)** — a `FinalRiskResponse` (§7.8).

---

### 7.8 `FinalRiskResponse` fields

Shape returned by `POST /api/v1/risks` and by the risk branches of `POST /api/v1/ingest` and `POST /api/v1/ingest/risks` (`../src/InsuranceIntegration.Api/FinalMessages/Risks/FinalRiskResponse.cs`).

| Field | Type | Notes |
|---|---|---|
| `entityId` | GUID | Platform-assigned id for the submission |
| `externalReference` | string | Source-provided reference |
| `productCode` | string | Canonical product code |
| `sourceSystem` | string | Ingest origin |
| `transactionType` | string | `Submission` \| `Quote` \| `PolicyBind` \| ... |
| `submissionStatus` | string | Outcome of submission checks |
| `quoteStatus` | string | `Quoted` \| `Blocked` \| ... |
| `policyStatus` | string | `ReadyToBind` \| ... |
| `brokerDecision` | string | Decision label |
| `insuredDecision` | string | `AcceptableInsured` \| `Decline` \| ... |
| `claimCount`, `sectionCount`, `installmentCount`, `sectionOperationCount`, `subcoverOperationCount`, `blockingEnrichmentCount` | int | Counters |
| `clearanceDecision` | string | `AutoCleared` \| `ManualClearance` \| ... |
| `autoCleared` | bool | True when `clearanceDecision == AutoCleared` |
| `totalIncurredAmount`, `totalReservedAmount` | decimal | Claim aggregates |
| `basePremium`, `adjustedPremium` | decimal | Premium after enrichments |
| `bestFuzzyMatchDistance` | int | Levenshtein distance to nearest known submission |
| `bestFuzzyMatchDescription` | string | Human-readable match label |
| `decisionReasons`, `appliedEnrichments`, `sectionActions` | string[] | Audit trails |
| `finalStatus` | string | e.g. `ReadyForDownstreamDispatch` |

---

### 7.9 `POST /api/v1/policies/cancellations`

Computes a pro-rata or short-rate cancellation (`../src/InsuranceIntegration.Api/Endpoints/PolicyEndpoints.cs:9-13`, `../src/InsuranceIntegration.Api/Services/Policies`).

**Request body** (`CancellationRequest`)

```json
{
  "policyReference": "POL-7781",
  "annualPremium": 12000.00,
  "inceptionDate": "2026-01-01",
  "expiryDate": "2026-12-31",
  "cancellationDate": "2026-07-01",
  "basis": "ProRata",
  "shortRatePenaltyPercent": 0.10,
  "minimumRetainedPremium": 0
}
```

`basis` accepts `ProRata` or `ShortRate` (`../src/InsuranceIntegration.Api/Services/Policies/CancellationBasis.cs`).

**Response (200 OK)** (`CancellationResult`)

```json
{
  "policyReference": "POL-7781",
  "earnedPremium": 5967.03,
  "unearnedPremium": 6032.97,
  "returnPremium": 6032.97,
  "shortRatePenalty": 0,
  "retainedPremium": 5967.03,
  "basis": "ProRata",
  "reasons": [
    "Earned fraction: 0.4973 over 364 days",
    "Earned premium: 5967.03",
    "Unearned premium: 6032.97",
    "Basis applied: ProRata",
    "No short-rate penalty applied",
    "Return premium: 6032.97",
    "Retained premium: 5967.03"
  ]
}
```

With `"basis": "ShortRate"` a penalty of `unearnedPremium * shortRatePenaltyPercent` is withheld from the return premium.

---

### 7.10 `POST /api/v1/policies/endorsements`

Computes the pro-rata premium delta for a mid-term endorsement (`../src/InsuranceIntegration.Api/Endpoints/PolicyEndpoints.cs:15-19`).

**Request body** (`EndorsementRequest`)

```json
{
  "policyReference": "POL-7781",
  "currentAnnualPremium": 12000.00,
  "newAnnualPremium": 13500.00,
  "inceptionDate": "2026-01-01",
  "expiryDate": "2026-12-31",
  "effectiveDate": "2026-07-01"
}
```

**Response (200 OK)** (`EndorsementResult`)

```json
{
  "policyReference": "POL-7781",
  "premiumDelta": 1500.00,
  "proRataAdjustment": 754.12,
  "adjustmentDirection": "AdditionalPremium",
  "reasons": [
    "Premium delta: 1500",
    "Remaining days: 183 of 364",
    "Pro-rata adjustment: 754.12",
    "Direction: AdditionalPremium"
  ]
}
```

`adjustmentDirection` is one of `AdditionalPremium`, `ReturnPremium`, or `NoAdjustment`.

---

### 7.11 Schema endpoints

Each schema endpoint returns the JSON Schema document for the referenced C# type (`../src/InsuranceIntegration.Api/Endpoints/SchemaEndpoints.cs`, `../src/InsuranceIntegration.Api/Services/Schemas`).

```powershell
Invoke-RestMethod http://localhost:5000/api/v1/schemas/ingest/envelope | ConvertTo-Json -Depth 10
Invoke-RestMethod http://localhost:5000/api/v1/schemas/ingest/risk-request
Invoke-RestMethod http://localhost:5000/api/v1/schemas/canonical/risk-request
Invoke-RestMethod http://localhost:5000/api/v1/schemas/final/risk-response
```

Use these for contract validation, onboarding, or code-generation in clients.

## 8. Quick recipes

### Trace a request end-to-end

```powershell
$corr = [guid]::NewGuid().ToString()
Invoke-RestMethod `
  -Uri http://localhost:5000/health `
  -Headers @{ "X-Correlation-Id" = $corr }
```

The same `X-Correlation-Id` value appears in the response header and in the server-side log scope attached to that request.

### Exercise idempotency

Send the same envelope twice with the same `id` + `source`; the `handlerName` and `result` fields will be identical, and the server logs will not show a second pass through the handler (see `../tests/InsuranceIntegration.Api.Tests/Ingest/IdempotencyDispatchTests.cs` for the test that asserts this).

### Observe the outbox

Any flow that writes to the outbox results in an `OutboxDispatcher` log line shaped like:

```
Dispatching outbox event <EventType> for <AggregateType> <AggregateId> (EventId=..., CorrelationId=...).
```

Rows are then marked dispatched in `OutboxMessages.DispatchedAtUtc`.

## 9. Manual testing pack

Ready-to-POST JSON payloads live in `./examples/`, and `./API_EXAMPLES.md` walks through each one with mandatory-field tables and the business rules it exercises (rich sections, sub-limits, deductibles, perils, exclusions, installment schedules, endorsement operations, etc.).

```powershell
Invoke-RestMethod `
  -Uri http://localhost:5000/api/v1/risks `
  -Method Post `
  -ContentType application/json `
  -InFile .\docs\examples\risks-canonical-auto-clear.json
```

## 10. Troubleshooting

- **`InvalidOperationException: No ingest handler registered...`** — the `(source, type)` combination is not supported. Consult the dispatch matrix in §7.5.
- **`InvalidOperationException: No risk mapper registered...`** — same issue for `POST /api/v1/ingest/risks`. Check casing of `sourceSystem` and `messageType` (matching is case-insensitive but the values must be one of the configured pairs).
- **`Unable to deserialize ... payload.`** — the `data` / `payload` object doesn't match the expected source-specific shape. Check the corresponding DTO under `../src/InsuranceIntegration.Api/SourceContracts`.
- **Swagger UI returns 404** — the app is running in a non-Development environment. Set `ASPNETCORE_ENVIRONMENT=Development` before running.
- **Database file locked on Windows** — stop all running instances of the API, then delete `integration.db*` files from the working directory to reset.
