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

The context (`IntegrationDbContext`) owns six tables:

| Table | Purpose |
|---|---|
| `KnownSubmissions` | Clearance registry for previously seen submissions |
| `IngestEntries` | Idempotency cache keyed by `(Source, EnvelopeId)` and queryable via `GET /api/v1/ingest/{source}/{envelopeId}` |
| `OutboxMessages` | Events awaiting dispatch by the background worker |
| `PolicySnapshots` | One row per policy with the JSON snapshot blob and a few denormalized columns for filterable list queries |
| `QuoteSnapshots` | Same shape as `PolicySnapshots`, keyed by quote reference |
| `DomainEvents` | Append-only log of canonical-shaped business events, queryable per aggregate (`Policy`/`Quote`) and per event type. Replay-source for `POST /api/v1/snapshots/.../rebuild`. |

Generated migrations live in `../src/InsuranceIntegration.Api/Migrations`.

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

`POST /api/v1/ingest` is idempotent per `(Source, Id)`. When the same envelope `Id` is sent again for the same `Source`, the dispatcher returns the previously stored `IngestReceipt` without re-running the handler (`../src/InsuranceIntegration.Api/Services/Ingest/IngestDispatcher.cs`). This is persisted in `IngestEntries` via `EfCoreIdempotencyStore` and can also be retrieved later with `GET /api/v1/ingest/{source}/{envelopeId}`.

### Outbox

Canonical flows (risk, claim, billing, compliance) can enqueue events via `IOutboxWriter` into the `OutboxMessages` table. The `OutboxDispatcher` background service polls every 2 seconds, batches up to 50 pending rows, logs a dispatch line per event, and marks them dispatched (`../src/InsuranceIntegration.Api/Services/Outbox/OutboxDispatcher.cs`). Real transport integration is not implemented yet — logs are the current sink.

### Domain snapshots (consolidated views per business key)

Each `POST /api/v1/ingest` produces an `IngestReceipt` for that one envelope (per-event view). On top of that, the platform also builds **per-business-key snapshots**: one growing document per quote and per policy, merged from every event we have seen about it.

| Snapshot | Key | Updated by | Read endpoint |
|---|---|---|---|
| `QuoteSnapshot` | `quoteReference` (or `externalReference` when no quote ref) | every Risk-domain ingest (Contoso RiskSubmission, QuoteForge QuoteRequest, BindPoint PolicyBindRequest) | `GET /api/v1/quotes/{quoteReference}` |
| `PolicySnapshot` | `policyReference` | any Risk-domain ingest that includes a `policyReference`, plus internal lifecycle ops (cancel / endorse / renew) | `GET /api/v1/policies/{policyReference}` |

The pipeline runs inside `RiskIngestHandler` after the flow service computes the `FinalRiskResponse`:

```
SourceIngestEnvelope
  → IRiskIngestMapper (source DTO → CanonicalRiskRequest)
  → IRiskFlowService.Process (decisions → FinalRiskResponse)
  → IRiskSnapshotRouter.Route
        ├── IDomainEventLog.Append (one event per affected aggregate)
        ├── IQuoteSnapshotService.Apply (when a quote ref is present and txn is not policy-lifecycle)
        └── IPolicySnapshotService.Apply (when policyReference is present)
  → IngestReceipt (returned to caller)
```

Sources `../src/InsuranceIntegration.Api/Services/Snapshots/RiskSnapshotRouter.cs`, `PolicySnapshotProjector.cs`, `QuoteSnapshotProjector.cs`.

**Merge rules** (`SnapshotMerge.cs`):

- Scalar string fields use last-write-wins **only if the new event has a value** — empty/null incoming values do not wipe existing data.
- Premium / coverage blocks are only overwritten when the new event actually carries that data (e.g. a billing-only event won't wipe coverage warnings).
- `History` is append-only — every event that touches the snapshot adds an entry recording `source`, `envelopeId`, `messageType`, and `transactionType`.
- `ExternalReferences` is a `Dictionary<sourceSystem, externalReference>` so you can see how each source identifies the same business entity.

**Smaller-source case**: a Contoso-only ingest produces a thin `QuoteSnapshot` with insured / premium hint / quote status filled but no broker block, no bind data, no claims. As more events arrive (a QuoteForge quote with broker info, a BindPoint bind that creates the policy), the same snapshot is enriched in place. Same shape for everyone — just partially filled until more is known.

**Persistence**: snapshots live in `PolicySnapshots` / `QuoteSnapshots` tables. Each row stores a `SnapshotJson` column with the full document plus a few denormalized columns (`ProductCode`, `UnderwritingYear`, `CurrentPhase`, `LastUpdatedUtc`, `IsBound`) for filterable list queries. Idempotent ingest replays do not double-append history because the dispatcher short-circuits at the receipt layer before the projector ever runs.

### Domain events (cross-aggregate timeline)

Alongside the snapshot writes, every business state change also appends a row to the `DomainEvents` table. This gives you a queryable cross-aggregate timeline that the snapshot's embedded `History[]` cannot — for example "every cancellation in the last week" or "every event for policy POL-7781 in arrival order".

| Column | Notes |
|---|---|
| `Id` | Primary key (Guid) |
| `EventType` | `RiskSubmissionReceived`, `QuoteIssued`, `QuoteBound`, `PolicyBound`, `PolicyEndorsed`, `PolicyCancelled`, `PolicyRenewed`, `PolicyReinstated` |
| `AggregateKind` | `Policy` or `Quote` |
| `AggregateKey` | the policy or quote reference |
| `Source` | `CONTOSO_UW`, `QUOTEFORGE`, `BINDPOINT`, ..., or `internal` for cancel / endorse / renew |
| `EnvelopeId` | the source envelope id when applicable; a synthetic id for internal events |
| `OccurredAtUtc` | when the source event happened |
| `RecordedAtUtc` | when this platform persisted the row |
| `PayloadJson` | a `RiskEventPayload { canonicalRequest, finalResponse }` — enough to deterministically replay through the projector |

The event log is the **source of truth for replay**. `POST /api/v1/snapshots/policies/{ref}/rebuild` (and the equivalent for quotes) reads every event for an aggregate, runs the projector across them in memory, and returns the rebuilt snapshot — useful for sanity-comparing against the live row, recovering after schema changes, or testing new projector logic against historical data.

Three storage tiers, three replay scopes:

| Tier | Replay from | Recovers |
|---|---|---|
| `IngestEntries` (raw envelopes) | source envelopes | full pipeline rebuild from scratch |
| `DomainEvents` (canonical events) | event payloads | any snapshot from its events |
| `PolicySnapshot` / `QuoteSnapshot` | the rebuilt state | fast reads |

Sources: `../src/InsuranceIntegration.Api/Services/Events/DomainEventLog.cs`, `../src/InsuranceIntegration.Api/Services/Snapshots/SnapshotRebuildService.cs`.

### Quote versioning, validity, and bind preconditions

Quotes are first-class versioned objects. The `QuoteSnapshot.Lifecycle` block carries:

- `Version` — increments on each quote-issuance event (Contoso `RiskSubmission`, QuoteForge `QuoteRequest`, ...). Stays put on bind / cancel / endorse.
- `IssuedAtUtc` — UTC of the most recent issuance.
- `ValidUntilUtc` — `IssuedAtUtc + ValidityDays` (default 30).
- `ValidityDays` — per-quote validity window.
- `BindRejectionReason` — populated when a bind attempt was rejected, cleared on a successful bind.

Before a `PolicyBind` produces a `Bound` policy, `BindPreconditionService` checks the existing quote and rejects when:

- the bind has no `quoteReference`
- no quote exists for that reference
- the quote is already bound to a different policy
- `now > ValidUntilUtc` (expired)
- the quote's `QuoteStatus` is not in `{Quoted, Indicative}`

A rejected bind produces `policyStatus=Draft`, `finalStatus=BindRejected`, and `bindRejectionReason` populated on the `FinalRiskResponse`. The QuoteSnapshot mirrors the rejection reason on `lifecycle.bindRejectionReason`. Source: `../src/InsuranceIntegration.Api/Services/Flows/BindPreconditionService.cs`.

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
| `GET` | `/api/v1/ingest/{source}/{envelopeId}` | Retrieve the persisted `IngestReceipt` for a previously processed envelope |
| `POST` | `/api/v1/risks` | Canonical risk submission (skips source mapping) |
| `POST` | `/api/v1/policies/cancellations` | Apply a cancellation to an existing policy (math + snapshot + `PolicyCancelled` event) |
| `POST` | `/api/v1/policies/endorsements` | Apply a mid-term endorsement (math + snapshot + `PolicyEndorsed` event) |
| `POST` | `/api/v1/policies/renewals` | Generate a renewal quote with loss-ratio and exposure-driven re-pricing |
| `GET` | `/api/v1/policies` | List recent policy snapshots (paginated, summary fields only) |
| `GET` | `/api/v1/policies/{policyReference}` | Full `PolicySnapshot` consolidated from all events for that key |
| `POST` | `/api/v1/snapshots/policies/{policyReference}/rebuild` | Replay the policy's `DomainEvents` through the projector and return the rebuilt snapshot |
| `GET` | `/api/v1/quotes` | List recent quote snapshots (paginated, summary fields only) |
| `GET` | `/api/v1/quotes/{quoteReference}` | Full `QuoteSnapshot` consolidated from all events for that key |
| `POST` | `/api/v1/snapshots/quotes/{quoteReference}/rebuild` | Replay the quote's `DomainEvents` through the projector and return the rebuilt snapshot |
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

**Response (200 OK)** — outer envelope is `IngestReceipt` with the handler's own output in `outcome` (for risks this is a `FinalRiskResponse`). The `self` link is what you can `GET` later to retrieve the same receipt; `(source, envelopeId)` together is the searchable key in the `IngestEntries` table:

```json
{
  "source": "CONTOSO_UW",
  "envelopeId": "evt-0001",
  "messageType": "RiskSubmission",
  "processedBy": "RiskIngestHandler",
  "correlationId": "7f3d...",
  "receivedAtUtc": "2026-04-26T01:30:00Z",
  "self": "/api/v1/ingest/CONTOSO_UW/evt-0001",
  "outcome": {
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

Shape returned by `POST /api/v1/risks` and by the risk branches of `POST /api/v1/ingest` and `POST /api/v1/ingest/risks` (`../src/InsuranceIntegration.Api/Responses/Risks/FinalRiskResponse.cs`).

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
| `finalStatus` | string | `ReadyForDownstreamDispatch`, `ManualUnderwritingReview`, `BindRejected`, or a policy-lifecycle status (`Cancelled` / `Endorsed` / `Renewed` / `Reinstated`) |
| `bindRejectionReason` | string \| null | Populated when a bind transaction failed metadata or precondition checks (quote missing / expired / not in a bindable status); null otherwise |

---

### 7.9 `POST /api/v1/policies/cancellations`

Applies a pro-rata or short-rate cancellation to a previously bound policy. Updates the `PolicySnapshot` to status `Cancelled` and writes a `PolicyCancelled` row to `DomainEvents` in the same EF transaction. Returns 404 if the policy reference is unknown, 400 if the input is invalid (`../src/InsuranceIntegration.Api/Services/Policies/PolicyLifecycleService.cs`).

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

**Response (200 OK)** (`PolicyLifecycleResult`)

```json
{
  "policyReference": "POL-7781",
  "transactionType": "Cancellation",
  "policyStatus": "Cancelled",
  "currentPhase": "Cancelled",
  "domainEventId": "6c6c6e2c-...",
  "domainEventType": "PolicyCancelled",
  "cancellation": {
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
  },
  "endorsement": null
}
```

With `"basis": "ShortRate"` a penalty of `unearnedPremium * shortRatePenaltyPercent` is withheld from the return premium.

---

### 7.10 `POST /api/v1/policies/endorsements`

Applies a mid-term endorsement to a previously bound policy. Updates the `PolicySnapshot` to status `Endorsed` and writes a `PolicyEndorsed` row to `DomainEvents`. 404 / 400 semantics match cancellations.

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

**Response (200 OK)** (`PolicyLifecycleResult`)

```json
{
  "policyReference": "POL-7781",
  "transactionType": "MidTermAdjustment",
  "policyStatus": "Endorsed",
  "currentPhase": "Endorsed",
  "domainEventId": "7d8c...",
  "domainEventType": "PolicyEndorsed",
  "cancellation": null,
  "endorsement": {
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
}
```

`adjustmentDirection` is one of `AdditionalPremium`, `ReturnPremium`, or `NoAdjustment`.

---

### 7.11 `POST /api/v1/policies/renewals`

Generates a renewal quote for a bound policy with premium re-priced from the prior term's loss ratio and the broker's exposure delta. Two domain events are produced in two distinct EF transactions:

1. The prior policy gets a `PolicyRenewed` event and its `PolicySnapshot` transitions to `policyStatus=Renewed`.
2. A fresh `QuoteSnapshot` is created for the new term with `lifecycle.version=1`, a fresh `validUntilUtc`, and `priorPolicyReference` set to the renewed policy. A `QuoteIssued` event is emitted on the new quote aggregate.

Source: `../src/InsuranceIntegration.Api/Services/Policies/PolicyRenewalService.cs`.

**Request body** (`RenewalRequest`)

```json
{
  "policyReference": "POL-7781",
  "newQuoteReference": "QT-RENEWAL-7781",
  "newInceptionDate": "2027-01-01",
  "newExpiryDate": "2027-12-31",
  "priorAnnualPremium": 10000.00,
  "priorClaimsPaid": 4500.00,
  "revenueDeltaPercent": 0.10,
  "overrideLoadPercent": null
}
```

Pricing model:

| Loss ratio | Band | Load |
|---|---|---|
| `< 30%` | Excellent | -5% |
| `30-60%` | Standard | 0% |
| `60-80%` | Loaded | +10% |
| `80-100%` | HeavilyLoaded | +25% |
| `> 100%` | Distressed | +40% |

`exposureLoad = revenueDeltaPercent / 2` (so +20% revenue → +10% premium load). `overrideLoadPercent` is an optional underwriter adjustment. Total renewal premium = `priorAnnualPremium * (1 + lossLoad + exposureLoad + overrideLoad)`.

**Response (200 OK)** (`RenewalResult`)

```json
{
  "priorPolicyReference": "POL-7781",
  "newQuoteReference": "QT-RENEWAL-7781",
  "priorAnnualPremium": 10000.00,
  "lossRatio": 0.45,
  "lossRatioBand": "Standard",
  "lossRatioLoadPercent": 0.00,
  "exposureLoadPercent": 0.05,
  "overrideLoadPercent": 0.00,
  "renewalPremium": 10500.00,
  "policyRenewedEventId": "7d8c...",
  "quoteIssuedEventId": "a1b2...",
  "reasons": [
    "Loss ratio: 0.4500 (Standard)",
    "Loss-ratio load: 0.00%",
    "Exposure load (50% of revenue delta +10.00%): +5.00%",
    "Override load: 0.00%",
    "Total load applied to prior premium 10000.00: +5.00%",
    "Renewal premium: 10500.00"
  ]
}
```

Returns 404 if the prior policy is unknown, 400 if it's already cancelled, already renewed, or the new dates are invalid. Once issued, the renewal quote can be bound through the standard `POST /api/v1/ingest` BindPoint flow — the bind preconditions on `validUntilUtc` and `quoteStatus` apply.

---

### 7.12 Snapshot read endpoints

Consolidated views per business key, fed by every Risk-domain ingest. See [Domain snapshots](#domain-snapshots-consolidated-views-per-business-key) for the merge semantics. Endpoints live in `../src/InsuranceIntegration.Api/Endpoints/PolicyReadEndpoints.cs` and `../src/InsuranceIntegration.Api/Endpoints/QuoteReadEndpoints.cs`.

**`GET /api/v1/policies/{policyReference}`** — returns the full `PolicySnapshot` (404 if no events have ever included this `policyReference`).

```powershell
Invoke-RestMethod http://localhost:5000/api/v1/policies/POL-7781
```

Response shape (abbreviated):

```json
{
  "policyReference": "POL-7781",
  "quoteReference": "QT-2201",
  "productCode": "COMMERCIAL_PROPERTY",
  "underwritingYear": 2026,
  "currencyCode": "USD",
  "insured":   { "name": "Northwind Storage Ltd", "tradingName": "Northwind Storage Ltd" },
  "broker":    { "code": "BRK-044", "name": "Harbor Broking" },
  "lifecycle": { "submissionStatus": "Received", "quoteStatus": "Quoted", "policyStatus": "Bound", "currentPhase": "Bound", "autoCleared": true, "finalStatus": "ReadyForDownstreamDispatch", "clearanceDecision": "AutoCleared" },
  "premium":   { "base": 12500.0, "adjusted": 13750.0 },
  "coverage":  { "sectionCount": 3, "totalSumInsured": 5000000.0, "totalSectionPremium": 12500.0, "premiumAllocationBalanced": true, "warnings": [] },
  "dates":     { "inceptionDate": "2026-05-01", "expiryDate": "2027-04-30", "boundDate": "2026-04-25" },
  "externalReferences": { "CONTOSO_UW": "Q-100045", "QUOTEFORGE": "QT-2201", "BINDPOINT": "POL-7781" },
  "history": [
    { "atUtc": "2026-04-25T03:10:00Z", "source": "BINDPOINT", "messageType": "PolicyBindRequest", "envelopeId": "evt-bp-001", "transactionType": "PolicyBind" }
  ],
  "lastUpdatedUtc": "2026-04-25T03:10:00Z"
}
```

**`GET /api/v1/policies?skip=0&take=100`** — paginated summary list (newest first). `take` is clamped to `[1..500]`; default 100. Each item has `policyReference`, `quoteReference`, `productCode`, `underwritingYear`, `currentPhase`, `lastUpdatedUtc`, and a `self` link to the full snapshot.

**`GET /api/v1/quotes/{quoteReference}`** — same shape as policy snapshot but keyed by quote reference, with `lifecycle.isBound` and a top-level `policyReference` populated once a bind event arrives. `lifecycle.currentPhase` advances to `Bound` at the same time.

**`GET /api/v1/quotes?skip=0&take=100`** — paginated summary list of quote snapshots.

---

### 7.13 Snapshot rebuild endpoints

Replay the aggregate's domain events through the projector in memory and return the rebuilt snapshot. Useful for sanity-checking the live snapshot row, recovering after a projector change, or testing new business rules against historical data without mutating the current state.

**`POST /api/v1/snapshots/policies/{policyReference}/rebuild`**

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/api/v1/snapshots/policies/POL-7781/rebuild
```

**`POST /api/v1/snapshots/quotes/{quoteReference}/rebuild`**

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/api/v1/snapshots/quotes/QT-2201/rebuild
```

Response shape (`SnapshotRebuildResult<TSnapshot>`):

```json
{
  "aggregateKind": "Policy",
  "aggregateKey": "POL-7781",
  "eventsApplied": 3,
  "firstEventAtUtc": "2026-04-25T03:10:00Z",
  "lastEventAtUtc": "2026-09-01T11:00:00Z",
  "snapshot": { /* fully-populated PolicySnapshot or QuoteSnapshot */ }
}
```

Returns 404 when no events exist for the aggregate. Source: `../src/InsuranceIntegration.Api/Services/Snapshots/SnapshotRebuildService.cs`.

---

### 7.14 Schema endpoints

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

Send the same envelope twice with the same `id` + `source`; the `processedBy`, `outcome`, and `receivedAtUtc` fields will be identical, and the server logs will not show a second pass through the handler. You can also confirm by calling `GET /api/v1/ingest/{source}/{envelopeId}` (the `self` URL from the receipt) — it returns the persisted `IngestReceipt` (see `../tests/InsuranceIntegration.Api.Tests/Ingest/IdempotencyDispatchTests.cs` and `EfCoreIdempotencyStoreTests.cs` for the assertions).

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
