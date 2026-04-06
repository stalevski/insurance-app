# Insurance Integration Platform

## Project goal

This repository contains a SaaS insurance ingest and transformation platform built as a modular monolith. The platform is designed to receive source-specific insurance messages, normalize them into platform-owned canonical contracts, execute lifecycle-aware processing logic, and emit final outbound messages for downstream consumers.

## Architectural direction

- **Platform**: ASP.NET Core Web API
- **Runtime**: .NET 10 LTS
- **Design**: SOLID, OOP, composition over inheritance, thin endpoints, orchestration through application flow services
- **Model separation**: source contracts, canonical contracts, and final outbound messages are explicitly separated
- **Architecture style**: modular monolith first, plugin-ready later
- **Code organization**: one class per file, namespaces aligned to folders, business logic isolated for future unit testing

## Target product direction

The platform is intended to grow into a multi-source SaaS insurance integration layer for insurers, MGAs, brokers, and ecosystem partners supporting:

- submissions
- quotes
- policies
- clearance
- claims
- billing and installments
- endorsements
- renewals
- cancellations
- reinstatements

## Current structure

```text
src/InsuranceIntegration.Api/
  Program.cs
  Configuration/
  Endpoints/
  SourceContracts/
  CanonicalContracts/
  FinalMessages/
  Mappers/
  Services/
    Catalog/
    Matching/
    Flows/
    Schemas/
  ReferenceData/
```

### Current module responsibilities

- **SourceContracts**
  - Source-facing DTOs such as the generic ingest envelope and invented Polaris risk payload.
- **CanonicalContracts**
  - Platform-owned normalized insurance contracts for internal processing.
- **FinalMessages**
  - Outbound processed response models for downstream use.
- **Mappers**
  - Source-specific mapping into canonical contracts.
- **Services**
  - Catalog lookup, matching, flow orchestration, and schema generation.
- **ReferenceData**
  - Reserved for future static lookups and shared business reference assets.

## Current endpoints

- `GET /health`
- `GET /api/v1/source-systems`
- `POST /api/v1/ingest/risks`
- `POST /api/v1/risks`
- `GET /api/v1/schemas/ingest/risk-request`
- `GET /api/v1/schemas/canonical/risk-request`
- `GET /api/v1/schemas/final/risk-response`

## JSON schema support

The API exposes machine-readable JSON schema documents for the main vertical slice contracts:

- `GET /api/v1/schemas/ingest/risk-request`
- `GET /api/v1/schemas/canonical/risk-request`
- `GET /api/v1/schemas/final/risk-response`

These exist so client integrators, internal tooling, future contract validation middleware, and automated onboarding workflows can inspect the platform contracts without coupling themselves to source-specific examples.

## Example canonical request

This example intentionally uses a neutral canonical contract, not a source-specific payload:

```json
{
  "entityId": "e6f3c24a-a61a-4b3b-9f1b-58d08ef6da52",
  "externalReference": "EXT-2026-0001",
  "productCode": "COMMERCIAL_PROPERTY",
  "sourceSystem": "BROKER_PORTAL",
  "transactionType": "Submission",
  "schemeCode": "STANDARD",
  "transactionTimestampUtc": "2026-04-24T03:00:00Z",
  "annualizedGrossPremium": 15000.00,
  "currencyCode": "USD",
  "underwriterName": "Platform Intake",
  "paymentMethod": "Invoice",
  "submission": {
    "underwritingYear": 2026,
    "channelCode": "Digital",
    "brokerPremium": 14850.00,
    "technicalPremium": 14500.00,
    "revenue": 2200000.00,
    "isRenewal": false
  },
  "broker": {
    "brokerCode": "BRK-100",
    "brokerName": "Summit Risk Partners",
    "hasDelegatedAuthority": false,
    "isPreferredPartner": true
  },
  "insured": {
    "fullName": "Northwind Storage Ltd",
    "tradingName": "Northwind Storage",
    "segmentCode": "SME",
    "annualRevenue": 2200000.00,
    "employeeCount": 40,
    "yearsInBusiness": 8
  },
  "quote": {
    "quoteReference": "QT-2001",
    "effectiveDate": "2026-05-01",
    "expiryDate": "2027-04-30",
    "quoteStatusHint": "Indicative"
  },
  "policy": {},
  "clearance": {
    "autoClearanceEnabled": true,
    "premiumThreshold": 25000.00,
    "fuzzyMatchTolerance": 3
  },
  "enrichments": [
    {
      "family": "Universal",
      "code": "TRIAGE_STANDARD",
      "description": "Universal triage baseline",
      "multiplier": 1.02,
      "isBlocking": false,
      "isDerived": false
    }
  ],
  "contractChecks": [
    {
      "code": "CONTRACT_SIGNALS",
      "isComplete": true,
      "description": "Core contract indicators present"
    }
  ],
  "complianceChecks": [
    {
      "code": "KYC_BASELINE",
      "isComplete": true,
      "description": "Baseline compliance record complete"
    }
  ],
  "parties": [
    {
      "role": "Insured",
      "name": "Northwind Storage Ltd"
    }
  ],
  "claims": [
    {
      "claimReference": "CLM-100",
      "claimantName": "Northwind Storage Ltd",
      "incurredAmount": 1200.00,
      "reservedAmount": 300.00
    }
  ],
  "sections": [
    {
      "sectionCode": "PROP",
      "sectionName": "Property Damage",
      "subcovers": [
        {
          "subcoverCode": "FIRE",
          "subcoverName": "Fire"
        }
      ]
    }
  ],
  "sectionOperations": [
    {
      "sectionCode": "PROP",
      "operationType": "AddSection",
      "subcoverCode": null,
      "removeAllSubcovers": false
    }
  ],
  "installments": [
    {
      "sequenceNumber": 1,
      "dueDate": "2026-06-01",
      "amount": 7500.00,
      "isPaid": false
    }
  ]
}
```

## Remaster direction

- **Inbound source-specific contracts** stay isolated from canonical processing contracts.
- **Canonical internal contracts** remain platform-owned and neutral.
- **Final outbound messages** remain separate from source and canonical DTOs.
- **Flow-specific logic** is organized by lifecycle area and can be expanded into claims, billing, clearance, policy, and endorsement slices.
- **Future pluginization** should be possible because boundaries already separate mapping, orchestration, matching, schema generation, and flow logic.

## Current implemented vertical slice

The current implementation delivers the first reusable risk ingest pattern:

- generic source ingest envelope
- Polaris risk mapper for `POLARIS_UW` + `RiskSubmission`
- canonical risk processing flow
- enrichment, premium, claim aggregation, matching, clearance, and section-action logic
- final risk response contract
- catalog and schema endpoints

## Build

```powershell
dotnet build .\InsuranceIntegration.sln
```
