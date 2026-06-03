# API Examples — Manual Testing

This guide pairs every `docs/examples/*.json` file with the endpoint it targets, the **mandatory** vs *optional* fields, and the business rules each scenario exercises. Use it together with `./USAGE.md` (which has the high-level run instructions).

> Prefer Postman? See [§9 Testing with Postman](#9-testing-with-postman) for the curated collection and three import paths.

> All `source` and `sourceSystem` codes used here (`POLARIS_UW`, `QUOTEFORGE`, `BINDPOINT`, `CLAIMFORGE`, `PAYMENTRAIL`, `SANCTIONSCAN`, `BROKER_PORTAL`, …) are **fictional** — they live only in this codebase's catalog (`src/InsuranceIntegration.Api/Services/Catalog/SourceSystemCatalogService.cs`). Three of them have real risk mappers (`POLARIS_UW`, `QUOTEFORGE`, `BINDPOINT`); the rest are echoed through verbatim so any string works.

## How to send an example

PowerShell (Windows):

```powershell
Invoke-RestMethod `
  -Uri http://localhost:5000/api/v1/ingest `
  -Method Post `
  -ContentType application/json `
  -InFile .\docs\examples\ingest-polaris-risk-submission.json
```

curl (works in pwsh too if you use `curl.exe`):

```powershell
curl.exe -X POST http://localhost:5000/api/v1/ingest `
  -H "Content-Type: application/json" `
  --data-binary "@docs/examples/ingest-polaris-risk-submission.json"
```

Add `-H "X-Correlation-Id: <guid>"` to trace the request through the logs.

---

## 1. `POST /api/v1/ingest` — generic envelope

### Envelope mandatory fields

| Field | Type | Notes |
|---|---|---|
| `id` | string | Unique per `source`. Drives idempotency. |
| `source` | string | e.g. `POLARIS_UW`. Some handlers route on `(source,type)`, others only on `type`. |
| `type` | string | Selects the handler — see the dispatch matrix in `./USAGE.md`. |
| `schemaVersion` | string | Caller-managed. |
| `occurredAtUtc` | RFC 3339 datetime | Source event time. |
| `data` | JSON object | Source-specific payload — shape depends on `(source,type)`. |

Optional: `correlationId` (string, GUID echoed into log scopes).

### 1.1 Polaris risk submission — `ingest-polaris-risk-submission.json`

Routes to `RiskIngestHandler` → `PolarisRiskMapper`. Polaris fills sensible defaults (large account loads, claims, sections) so even a tiny payload runs the full risk flow.

**Mandatory `data` fields**: `quoteId`, `insuredName`, `trade`.

**Optional `data` fields**: `estimatedPremium` (defaults to 0).

**What you'll see in the response**

- `result.transactionType = "Submission"`
- `result.productCode = "COMMERCIALPROPERTY"` (uppercased from `trade`)
- `result.appliedEnrichments` includes `Property:GEO_CAT`, `Property:BUILDING_PROFILE` (product-derived), plus the universal triage signals.

### 1.2 QuoteForge quote request — `ingest-quoteforge-quote-request.json`

Routes to `RiskIngestHandler` → `QuoteForgeRiskMapper`.

**Mandatory `data` fields**: `quoteReference`, `insuredName`, `productLine`.

**Optional `data` fields**: `brokerCode`, `brokerName`, `technicalPremium`, `brokerPremium`, `currencyCode` (default `USD`), `effectiveDate`, `expiryDate`, `underwritingYear`, `insuredRevenue`, `insuredEmployeeCount`, `insuredYearsInBusiness`.

**Business rules wired in by the mapper**

- `transactionType = "Quote"` → `quoteStatus` becomes `Indicative` if premium > 0, else `NotQuoted`.
- `IsPreferredPartner` is set to `true` whenever `brokerCode` is present.
- Auto-clearance threshold is configured to `50000` and fuzzy-match tolerance to `3`.

### 1.3 BindPoint policy bind — `ingest-bindpoint-policy-bind.json`

Routes to `RiskIngestHandler` → `BindPointRiskMapper`. Mapper builds an installment schedule from `installmentCount` and `boundPremium`.

**Mandatory `data` fields**: `policyReference`, `quoteReference`, `insuredName`, `productCode`.

**Optional `data` fields**: `brokerCode`, `brokerName`, `brokerHasDelegatedAuthority`, `brokerIsPreferredPartner`, `boundPremium`, `currencyCode`, `inceptionDate`, `expiryDate`, `boundDate`, `paymentMethod`, `installmentCount`.

**Business rules**

- `transactionType = "PolicyBind"` → `policyStatus` becomes `Bound` when broker is delegated/preferred AND insured is acceptable; otherwise `Draft`.
- `quote.expiryDate` and `policy.expiryDate` flow from `expiryDate`.

### 1.4 Claim notice — `ingest-claim-notice-low-severity.json`, `ingest-claim-notice-high-severity.json`, `ingest-claim-notice-with-deductible.json`

Routes to `ClaimIngestHandler` (matched on `type` only — `source` can be any string).

**Mandatory `data` fields**: `claimReference`, `policyReference`, `claimantName`.

**Optional `data` fields**: `lossDate`, `lossCause`, `estimatedIncurred`, `estimatedReserved`, `paidAmount`, `currencyCode`, `fraudIndicator`, `affectedSectionCode`, `affectedSubcoverCode`, `affectedPerilCode`, `deductibleApplied`, `perOccurrenceLimit`.

**Business rules**

- **Severity** by `estimatedIncurred`: `< 5000` → `Low`, `< 50000` → `Medium`, otherwise `High`.
- **Triage**: `fraudIndicator || severity == High` → `SuspectedFraud` ⁄ `Escalate`; `Medium` → `ManualReview`; `Low` → `AutoProcess`.
- **Auto-close** only when `severity == Low`, no fraud flag, and `paidAmount >= estimatedIncurred`.
- **Deductible math**: `deductibleAmount = min(estimatedIncurred, deductibleApplied)`. `indemnityAmount = max(0, estimatedIncurred - deductibleAmount)`, then capped at `perOccurrenceLimit` if supplied (`limitBreached = true`).
- The `affectedSectionCode` / `affectedSubcoverCode` / `affectedPerilCode` are passed through to the response for downstream allocation.

The `with-deductible` example produces `deductibleAmount=5000`, `indemnityAmount=25000`, `limitBreached=false`. Replace `perOccurrenceLimit` with `20000` and you'll see `indemnityAmount=20000` with `limitBreached=true`.

### 1.5 Installment schedule — `ingest-installment-schedule-current.json`, `ingest-installment-schedule-delinquent.json`

Routes to `BillingIngestHandler`.

**Mandatory `data` fields**: `policyReference`, `installmentCount`, `totalAmount`.

**Optional `data` fields**: `currencyCode`, `firstDueDate`, `paidToDate`, `missedPayments`, **`installments[]`**.

Each `installments` entry:

| Field | Type | Notes |
|---|---|---|
| `sequenceNumber` | int | Required for ordering. |
| `dueDate` | date | `YYYY-MM-DD`. |
| `amount` | decimal | Per-installment amount. |
| `status` | string | `Planned` \| `Issued` \| `Paid` \| `Overdue` \| `Cancelled`. |
| `issuedDate`, `paidDate` | date? | Audit-trail only. |
| `paymentReference` | string? | Optional. |

**Business rules (when `installments[]` is provided)**

- `paidToDate` and `missedPayments` from the body are **ignored** — the service derives them from `status`.
- `totalAmount` is **derived** as the sum of non-`Cancelled` installments.
- `nextDueDate` = first installment whose status is `Planned` / `Issued` / `Overdue`, ordered by `sequenceNumber` then `dueDate`.
- `overdueInstallmentNumbers` is the list of `sequenceNumber` values whose status is `Overdue`.
- **Dunning** triggers at ≥ 1 overdue. **Non-payment cancellation** triggers at ≥ 3 overdue → `finalStatus = "PendingNonPaymentCancellation"`.

The `current` example has 0 overdue → `BillingStatus = Current`, `DunningTriggered = false`. The `delinquent` example has 3 overdue → `BillingStatus = SeverelyDelinquent`, `NonPaymentCancellationRecommended = true`.

If you omit `installments[]` entirely the service falls back to the legacy behavior (uses the top-level `paidToDate` and `missedPayments`). Both shapes coexist.

### 1.6 Compliance result — `ingest-compliance-result-clear.json`, `ingest-compliance-result-sanctions.json`, `ingest-compliance-result-edd.json`

Routes to `ComplianceIngestHandler`.

**Mandatory `data` fields**: `partyName`, `screeningResult`.

**Optional `data` fields**: `score`, `isPoliticallyExposed`, `hasSanctionsHit`, `entityReference`.

**Business rules** (first match wins)

| Condition | Decision |
|---|---|
| `hasSanctionsHit == true` | `SanctionsBlock` (`blocksBind = true`, `finalStatus = "Blocked"`) |
| `isPoliticallyExposed == true` | `EnhancedDueDiligence` (`requiresEnhancedDueDiligence = true`) |
| `screeningResult == "Flagged"` | `ManualReview` |
| `score >= 50` | `EnhancedDueDiligence` |
| otherwise | `Clear` |

---

## 2. `POST /api/v1/ingest/risks` — source-shape risk

Mandatory: `sourceSystem`, `messageType`, `payload`. Allowed `(sourceSystem, messageType)` pairs: `POLARIS_UW`/`RiskSubmission`, `QUOTEFORGE`/`QuoteRequest`, `BINDPOINT`/`PolicyBindRequest`. Anything else throws `InvalidOperationException`.

Example: `ingest-risks-quoteforge.json`. Same QuoteForge payload as §1.2 but bypasses idempotency (the generic dispatcher is not involved).

---

## 3. `POST /api/v1/risks` — canonical risk submission

The most expressive endpoint. It accepts the platform-neutral `CanonicalRiskRequest` directly. Use this when you want to drive a specific business outcome.

### Mandatory top-level fields

| Field | Type |
|---|---|
| `externalReference` | string |
| `productCode` | string |
| `sourceSystem` | string |
| `transactionType` | string |

Everything else has a default. `entityId` defaults to `Guid.Empty`; supply your own if you want the response `entityId` to round-trip.

### Mandatory nested fields

There aren't any — the inner objects (`submission`, `broker`, `insured`, `quote`, `policy`, `clearance`) all have parameterless defaults. In practice you should fill at least:

- `submission.underwritingYear`, `submission.brokerPremium` *or* `submission.technicalPremium` *or* the top-level `annualizedGrossPremium` — otherwise base premium is `0` and `quoteStatus` falls to `NotQuoted`.
- `broker.brokerCode` + `isPreferredPartner` (or `hasDelegatedAuthority`) — otherwise `brokerDecision` becomes `UnknownBroker`/`ManualBrokerReview`, blocking auto-clear.
- `insured.fullName`, `insured.yearsInBusiness >= 2` — otherwise `insuredDecision` becomes `ReferSeniorUnderwriter`/`UnknownInsured`.
- `clearance.autoClearanceEnabled = true` plus `premiumThreshold` and `fuzzyMatchTolerance` — required for `AutoCleared`.

### Coverage structure (sections / subcovers / perils)

The richer the shape, the more business rules fire. Canonical structure:

```jsonc
{
  "sections": [
    {
      "sectionCode": "PROP",
      "sectionName": "Property Damage",
      "status": "Active",                  // Active | Suspended | Removed (Removed sections are excluded from totals)
      "sumInsured": 750000.00,
      "sectionPremium": 1000.00,
      "warranties": [ "..." ],
      "specialConditions": [ "..." ],
      "subcovers": [
        {
          "subcoverCode": "FIRE",
          "subcoverName": "Fire and Allied Perils",
          "sumInsured": 750000.00,
          "perOccurrenceLimit": 750000.00,
          "aggregateLimit": 1500000.00,
          "deductible": 1000.00,
          "deductibleType": "Flat",       // Flat | PercentageOfLoss | PercentageOfSumInsured
          "premium": 1000.00,
          "perils": [
            { "code": "FIRE", "name": "Fire", "isCovered": true,  "subLimit": 750000.00, "waitingPeriodDays": 0 },
            { "code": "FLOOD", "name": "Flood", "isCovered": false }
          ],
          "exclusions": [ "EARTHQUAKE", "WAR" ]
        }
      ]
    }
  ]
}
```

**Coverage business rules (added in this revision)**

| Rule | Effect |
|---|---|
| `Status = Removed` sections | Excluded from `totalSumInsured` / `totalSectionPremium`. |
| Active section premium ≠ sum of subcover premiums (>1% drift) | `coverageWarnings[]` entry + non-blocking `Coverage:STRUCTURE_WARNING` enrichment. |
| `subcover.deductible > subcover.sumInsured` (when sum insured > 0) | Coverage warning. |
| `subcover.aggregateLimit < subcover.perOccurrenceLimit` | Coverage warning. |
| `subcover.deductibleType` not in `{Flat, PercentageOfLoss, PercentageOfSumInsured}` | Coverage warning. |
| `peril.subLimit > subcover.sumInsured` | Coverage warning. |
| Claim has `affectedPerilCode` AND that peril is in `subcover.exclusions` OR has `isCovered == false` (or no matching peril at all when the subcover has perils declared) | **Blocking** `Coverage:UNCOVERED_PERIL_CLAIM` → `quoteStatus = Blocked`, `insuredDecision = Decline`. |
| `totalSectionPremium` differs from `basePremium` by > 1% | `premiumAllocationBalanced = false` (signal only — does not block). |

### Scenarios

| File | Outcome | Why |
|---|---|---|
| `risks-canonical-auto-clear.json` | `clearanceDecision = AutoCleared`, `quoteStatus = Quoted`, `policyStatus = ReadyToBind` | All clearance preconditions satisfied; rich subcover structure with a covered claim. |
| `risks-canonical-manual-clearance.json` | `clearanceDecision = ManualClearance`, `autoCleared = false` | `contractChecks[0].isComplete = false` flips the contract-completeness gate. |
| `risks-canonical-decline.json` | `insuredDecision = Decline`, `quoteStatus = Blocked` | One claim with `incurredAmount = 30000` exceeds the $25,000 decline threshold. |
| `risks-canonical-uncovered-peril-claim.json` | Blocked via the new uncovered-peril rule | Claim references peril `FLOOD`, which is in the subcover's `exclusions[]`. |
| `risks-canonical-multi-section.json` | `quoteStatus = Quoted`, premium allocation balanced across 3 sections | Property + BI + Liability with realistic limits, perils, deductibles, warranties. |

### Decision-engine reference (carried over from the existing flow)

- **Base premium** = `submission.brokerPremium` ?? `submission.technicalPremium` ?? `annualizedGrossPremium` ?? `0`.
- **Adjusted premium** = base × product of all enrichment multipliers (universal, product-derived, coverage).
- **Auto-clearance requires all of**:
  - `clearance.autoClearanceEnabled = true`
  - `adjustedPremium <= clearance.premiumThreshold`
  - sum of incurred claim amounts ≤ 5000
  - all `contractChecks[].isComplete` AND all `complianceChecks[].isComplete`
  - best fuzzy-match distance ≤ `clearance.fuzzyMatchTolerance`
  - `blockingEnrichmentCount == 0`
  - broker is `DelegatedBindAuthority` or `PreferredBroker`
  - insured is `AcceptableInsured` (full name present, ≥ 2 years in business, no decline trigger)

---

## 4. `POST /api/v1/policies/cancellations`

**Mandatory body fields**: `policyReference`, `annualPremium`, `inceptionDate`, `expiryDate`, `cancellationDate`.

**Optional**: `basis` (default `ProRata`), `shortRatePenaltyPercent` (default `0.10`), `minimumRetainedPremium` (default `0`).

Examples (already in `docs/examples/`): create from the templates in `./USAGE.md` §7.9. Pro-rata math with the standard sample yields `earnedPremium = 5967.03`, `unearnedPremium = 6032.97`. Switching to `"basis": "ShortRate"` with `0.10` penalty withholds 10% of the unearned portion.

---

## 5. `POST /api/v1/policies/endorsements`

**Mandatory body fields**: `policyReference`, `currentAnnualPremium`, `newAnnualPremium`, `inceptionDate`, `expiryDate`, `effectiveDate`.

**Optional**: `sectionOperations[]` (new — see below).

### Section operations

Each `sectionOperations[]` entry:

| Field | Type | Notes |
|---|---|---|
| `operationType` | string | `AddSection` \| `RemoveSection` \| `AddSubcover` \| `RemoveSubcover` \| `UpdateLimit` \| `UpdateDeductible` |
| `sectionCode` | string | Required. |
| `subcoverCode` | string? | Required for subcover-scoped operations. |
| `sumInsuredDelta` | decimal | Aggregated into `result.sumInsuredDelta`. |
| `deductibleDelta` | decimal | Aggregated into `result.deductibleDelta`. |
| `premiumDelta` | decimal | Documentation-only — premium math comes from `currentAnnualPremium` → `newAnnualPremium`. |
| `reason` | string? | Free-text reason rendered into `operationsApplied[]`. |

Example file: `policies-endorsement-with-section-operations.json`. With the standard 2026 calendar and `effectiveDate = 2026-07-01` it returns:

- `premiumDelta = 1800`, `proRataAdjustment ≈ 904.95`, `adjustmentDirection = "AdditionalPremium"`
- `sumInsuredDelta = 750000`, `deductibleDelta = -2500`
- `operationsApplied[0]` ≈ `"UpdateLimit on PROP/BUILDINGS [sum insured +500000, premium +1200] (New warehouse extension added to declarations)"`

Endorsements with no `sectionOperations[]` keep the prior behavior (premium math only, empty `operationsApplied`).

---

## 6. `GET /api/v1/products/{productCode}/rating`

Pure GET — no JSON body. Mandatory query parameter `annualRevenue`. Optional `currencyCode` (default `USD`).

```powershell
Invoke-RestMethod "http://localhost:5000/api/v1/products/COMMERCIAL_PROPERTY/rating?annualRevenue=2200000"
```

**Business rules**

- `revenueBasedPremium = round(annualRevenue / 1000 * baseRatePerThousandRevenue, 2)`.
- If `annualRevenue >= largeAccountThreshold`, a `largeAccountLoadAmount` of `revenueBasedPremium * largeAccountLoad` is added.
- `technicalPremium = max(revenueBasedPremium + load, minimumPremium)`.

Catalog values (`COMMERCIAL_PROPERTY`, `LIABILITY`, `CYBER`, `MOTOR`) are documented in `./USAGE.md` §7.3.

---

## 7. Field-mandatoriness cheat sheet

| Endpoint | Mandatory body fields |
|---|---|
| `POST /api/v1/ingest` | `id`, `source`, `type`, `schemaVersion`, `occurredAtUtc`, `data` |
| `POST /api/v1/ingest/risks` | `sourceSystem`, `messageType`, `payload` |
| `POST /api/v1/risks` | `externalReference`, `productCode`, `sourceSystem`, `transactionType` |
| `POST /api/v1/policies/cancellations` | `policyReference`, `annualPremium`, `inceptionDate`, `expiryDate`, `cancellationDate` |
| `POST /api/v1/policies/endorsements` | `policyReference`, `currentAnnualPremium`, `newAnnualPremium`, `inceptionDate`, `expiryDate`, `effectiveDate` |

Mandatory **`data` / `payload` fields** by `(source, type)` are listed in §1 above and again per source in `./USAGE.md` §7.5–7.6.

---

## 8. End-to-end test plan

A repeatable smoke run that exercises every flow extension (assumes the API at `http://localhost:5000`):

```powershell
$base = "http://localhost:5000"

# 1. Auto-clearable canonical risk with rich coverage
Invoke-RestMethod -Uri "$base/api/v1/risks" -Method Post -ContentType application/json `
  -InFile .\docs\examples\risks-canonical-auto-clear.json | Format-List finalStatus, totalSumInsured, premiumAllocationBalanced, coverageWarnings

# 2. Uncovered-peril claim must block
Invoke-RestMethod -Uri "$base/api/v1/risks" -Method Post -ContentType application/json `
  -InFile .\docs\examples\risks-canonical-uncovered-peril-claim.json | Format-List quoteStatus, appliedEnrichments

# 3. High-severity claim with deductible
Invoke-RestMethod -Uri "$base/api/v1/ingest" -Method Post -ContentType application/json `
  -InFile .\docs\examples\ingest-claim-notice-with-deductible.json | Select-Object -ExpandProperty result | Format-List severity, deductibleAmount, indemnityAmount, limitBreached

# 4. Delinquent installment schedule
Invoke-RestMethod -Uri "$base/api/v1/ingest" -Method Post -ContentType application/json `
  -InFile .\docs\examples\ingest-installment-schedule-delinquent.json | Select-Object -ExpandProperty result | Format-List billingStatus, overdueInstallmentNumbers, nextDueDate

# 5. Endorsement with section operations
Invoke-RestMethod -Uri "$base/api/v1/policies/endorsements" -Method Post -ContentType application/json `
  -InFile .\docs\examples\policies-endorsement-with-section-operations.json | Format-List sumInsuredDelta, deductibleDelta, operationsApplied

# 6. Sanctions block
Invoke-RestMethod -Uri "$base/api/v1/ingest" -Method Post -ContentType application/json `
  -InFile .\docs\examples\ingest-compliance-result-sanctions.json | Select-Object -ExpandProperty result | Format-List decision, blocksBind, finalStatus
```

Each step's expected outcomes are described in §1–§5 above.

---

## 9. Testing with Postman

Three options, in increasing order of setup effort and expressiveness.

### 9.1 Option A — Import the curated collection (recommended)

Two files live under `./postman/`:

- `./postman/InsuranceIntegration.postman_collection.json` — every endpoint plus the key business scenarios from §1–§5, organized in folders (`Health & catalog`, `Schemas`, `Ingest - Risk`, `Ingest - Claims`, `Ingest - Billing`, `Ingest - Compliance`, `Risk - Canonical`, `Policies`).
- `./postman/InsuranceIntegration.postman_environment.json` — sets `baseUrl` to `http://localhost:5000`.

Steps:

1. In Postman, **File → Import** and drop both files in (or use **Import** in the workspace sidebar).
2. Top-right environment selector → choose **InsuranceIntegration.Api - Local**.
3. Start the API (`dotnet run --project .\src\InsuranceIntegration.Api\InsuranceIntegration.Api.csproj`).
4. Open any request and hit **Send**.

What the collection does for you automatically (via a collection-level pre-request script):

- Generates a fresh `X-Correlation-Id` (`{{$guid}}`) on every send so each call is traceable in the API logs.
- Generates a unique envelope `id` (`evt-<timestamp>`) for ingest requests, so re-sending creates new envelopes instead of hitting idempotency.
- Stamps each request with the current UTC timestamp via `{{nowUtc}}`.

To **deliberately** test idempotency, replace `{{envelopeId}}` in the request body with a fixed string (e.g. `"id": "evt-fixed-1"`) and send the same request twice — the second response will have an identical `result.entityId`.

### 9.2 Option B — Import the live OpenAPI document

If you'd rather generate every endpoint from the live API:

1. Start the API in `Development` mode.
2. In Postman: **File → Import → Link** and paste `http://localhost:5000/openapi/v1.json`.
3. Postman creates a collection mirroring the OpenAPI document. You'll get all routes but no pre-filled request bodies — you'll fill those in manually (use the templates in §1–§5 or copy from `./examples/*.json`).

Trade-offs vs option A: always in sync with the latest endpoints, but no scenario-specific bodies, no pre-request scripts, and no business-rule annotations.

### 9.3 Option C — Manual request from `docs/examples/*.json`

For one-off testing without importing anything:

1. **New Request** → set method to `POST` and URL to e.g. `http://localhost:5000/api/v1/risks`.
2. **Headers** tab → add `Content-Type: application/json` and (optionally) `X-Correlation-Id: <your-guid>`.
3. **Body** tab → choose **raw** + **JSON**, then paste the contents of any `docs/examples/*.json` file.
4. **Send**.

### 9.4 Postman tips that matter for this API

- **Idempotency on ingest**: only the `(source, id)` pair is keyed. If you reuse the same `id` you'll get the cached `IngestReceipt` back without the handler running again. The collection's pre-request script avoids this by default; remove the script (Collection → Pre-request Script tab) if you want idempotency to kick in across runs.
- **Verify what was stored**: every successful `POST /api/v1/ingest` returns an `IngestReceipt` containing `source`, `envelopeId`, `receivedAtUtc`, and a `self` link. Either `GET` the `self` URL to retrieve the persisted receipt, or query the `IngestEntries` table directly with `WHERE Source='...' AND EnvelopeId='...'`.
- **Trace a request end-to-end**: copy `X-Correlation-Id` from the response headers and grep your server console for that GUID — every log line emitted while handling the request is scoped with it.
- **`X-Causation-Id`**: optional second header (also a GUID). Add it manually under any request's **Headers** tab when you want to model a downstream call caused by an upstream event; it joins the log scope but does not change behavior.
- **Schema responses are large**: the `/api/v1/schemas/*` endpoints return JSON Schema documents. In Postman use **Pretty** view and the JSONPath filter (`$..properties.foo`) to navigate.
- **Collection runner**: select the collection → **Run** → choose the *Risk - Canonical* folder (or *Ingest - Risk*) for a single-click smoke run that exercises auto-clear, manual-clearance, decline, and uncovered-peril scenarios in order.
- **Capture entity ids across requests**: add a **Tests** script to a request like the BindPoint bind:
  ```javascript
  pm.test("captures policy reference", function () {
      const json = pm.response.json();
      pm.collectionVariables.set("policyReference", json.result?.externalReference || "");
  });
  ```
  Subsequent requests can reference `{{policyReference}}` in their URL or body.

