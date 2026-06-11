# Known issues

Catalogue of bugs and business-logic concerns found during code analysis. **These are documented
here intentionally and are NOT yet fixed** — fixing is a separate, later pass (see `PROGRESS.md`).
Some odd logic may be deliberate "bait" for the QA testing-ground purpose; those are flagged.

Each item lists: location, what the code does, why it's a problem, severity, and a suggested fix.
Line numbers were verified against the codebase on 2026-06-03 but may drift as the code changes —
treat them as a starting point and confirm before fixing.

Legend: 🔴 High · 🟠 Medium · 🟡 Low · 🔵 Debatable / possibly by-design · ✅ Fixed

---

## 🔴 High

_None open. H1–H3 were fixed on 2026-06-11 — see the Fixed section below._

---

## 🟠 Medium

### M1 — Billing next-due-date derived from missed-payment count (no-schedule path)
- **Where:** `Services/Flows/BillingFlowService.cs`, around line 58:
  `nextDueDate = request.FirstDueDate?.AddMonths(Math.Max(request.MissedPayments, 0))`.
- **What:** When there is no explicit installment schedule, the next due date is the first due date
  plus *the number of missed payments* in months.
- **Why it matters:** Number of missed payments is unrelated to the billing cadence; this yields
  arbitrary due dates (e.g. 5 missed → first due + 5 months). The scheduled path (using the next
  open installment's `DueDate`) is correct; only the fallback is wrong.
- **Suggested fix:** Derive next due date from the billing frequency/cycle, not the missed count.

### M3 — Hard-coded, inconsistent underwriting thresholds
- **Where:** `Services/Flows/RiskFlowService.cs` — auto-clearance requires `totalIncurred <= 5000m`
  (line ~555) while insured decline triggers at `totalIncurred > 25000m` (line ~365).
- **What:** Two unrelated hard-coded money thresholds govern related decisions, leaving a 5k–25k
  band where behavior is implicit and untunable.
- **Why it matters:** Underwriting thresholds should be configuration/product-driven (as
  `Clearance.PremiumThreshold` already is). Hard-coded magic numbers are hard to reason about and
  tune, and the two values aren't obviously consistent.
- **Suggested fix:** Move both thresholds into configuration/product definitions and document the
  intended bands.

### M4 — `DateTime.UtcNow` used directly in logic (violates `TimeProvider` convention)
- **Where (examples):**
  - `Services/Flows/RiskFlowService.cs` line ~319 (`DateTime.UtcNow.Year`).
  - `Mappers/Risks/BindPointRiskMapper.cs` (~line 23), `PolarisRiskMapper.cs`,
    `QuoteForgeRiskMapper.cs`, and some ingest handlers.
- **What:** Wall-clock time is read directly instead of an injected `TimeProvider`.
- **Why it matters:** Non-deterministic; can cause year-boundary bugs and flaky tests. The repo
  convention (see `AGENTS.md`) is to inject `TimeProvider` and use `FakeTimeProvider` in tests.
- **Suggested fix:** Inject `TimeProvider` into these types and replace `DateTime.UtcNow`.

### M5 — Snapshot premium not cleared when an update sets premium to zero
- **Where:** `Services/Snapshots/PolicySnapshotProjector.cs` (premium update guarded by
  `> 0m` checks).
- **What:** Premium fields are only updated when the incoming value is `> 0`. A legitimate update to
  `0` (e.g. a waived policy) leaves the old premium in the snapshot.
- **Why it matters:** Stale premium in the read model → wrong reporting/analytics.
- **Suggested fix:** Distinguish "no premium provided" from "premium is zero" (e.g. nullable input)
  and apply zero when explicitly provided.

---

## 🟡 Low

### L1 — Polaris mapper hard-codes insured/broker data
- **Where:** `Mappers/Risks/PolarisRiskMapper.cs`.
- **What:** Broker name, insured revenue, employee count, and years-in-business are hard-coded
  constants; only insured name and estimated premium come from the payload.
- **Why it matters:** Any real values the source provides would be discarded; underwriting signals
  could be based on fabricated data. (May be acceptable as a demo/QA fixture.)
- **Suggested fix:** Map real fields from the source payload; keep constants only as explicit
  defaults where the source genuinely omits data.

### L2 — Non-deterministic `Guid.NewGuid()` for entity ids in mappers/handlers
- **Where:** `Mappers/Risks/*RiskMapper.cs` and several ingest handlers.
- **What:** `EntityId` is generated with `Guid.NewGuid()` inside mapping logic.
- **Why it matters:** Harder to assert in tests and to correlate across a pipeline run. Minor.
- **Suggested fix:** Generate ids at a single boundary, or inject an id/Guid provider for
  determinism in tests.

---

## 🔵 Debatable / possibly by-design

### D1 — BindPoint installment due dates start at inception
- **Where:** `Mappers/Risks/BindPointRiskMapper.cs` — `DueDate = InceptionDate.AddMonths(index - 1)`
  for `index` in `1..InstallmentCount`.
- **Note:** The initial analysis called this an off-by-one (last payment one month "early"). But a
  "pay first installment at inception, then monthly" schedule is a legitimate billing convention
  (12 installments from a Jan inception fall Jan–Dec). **Confirm the intended convention** before
  changing — it may be correct as-is.

### D2 — Pre-existing build warnings (38)
- **What:** `dotnet build` produces 38 analyzer/style warnings (nullable assignments, `CA1869`
  cached `JsonSerializerOptions`, `CA1707` migration naming, etc.). Build still succeeds.
- **Note:** Not bugs per se, but they erode the "no new warnings" bar. Worth a cleanup pass;
  migration-file warnings can be suppressed for generated code.

---

_When an item here is fixed, remove it (or move it to a "Fixed" section) and add a regression test._

---

## ✅ Fixed

### H1 — Outbox messages were marked dispatched but never actually sent _(fixed 2026-06-11)_
- Introduced `IOutboxPublisher` (default `LoggingOutboxPublisher`; swap the DI registration for a
  real transport). `OutboxDispatcher` now only sets `DispatchedAtUtc` after a successful publish;
  on failure it records `LastError`, leaves the message pending, and retries on later polls up to
  `MaxDispatchAttempts` (5), after which the message is poisoned and skipped.
- Regression tests: `tests/.../Outbox/OutboxDispatcherTests.cs` (publish success, failure leaves
  pending, attempt cap/poison, recovery after transient failure).

### H2 — Idempotency check was not atomic (TOCTOU race) _(fixed 2026-06-11)_
- `EfCoreIdempotencyStore.StoreAsync` now inserts first and treats the composite-key
  `(Source, EnvelopeId)` unique-constraint violation as "already processed" — first writer wins
  and its receipt is returned as the canonical outcome. `IIdempotencyStore.StoreAsync` returns the
  winning receipt; `IngestDispatcher` returns it to the caller.
- Regression test: `EfCoreIdempotencyStoreTests.StoreAsync_FirstWriterWins_OnConcurrentInsertForSameKey`.

### H3 — Renewal premium silently clamped to zero _(fixed 2026-06-11)_
- `PolicyRenewalService` now throws `ArgumentException` (→ 400 with the load breakdown in the
  message) when the computed renewal premium is negative, instead of silently producing a $0
  premium.
- Regression test: `PolicyRenewalServiceTests.ApplyRenewal_ThrowsWhenComputedRenewalPremiumIsNegative`.

### M2 — Installment rounding gap (totals didn't reconcile) _(fixed 2026-06-11)_
- `BindPointRiskMapper` now adds the rounding residual to the **last** installment so the schedule
  always sums exactly to `BoundPremium` (e.g. 1000 / 3 → 333.33 + 333.33 + 333.34).
- Regression tests: `BindPointRiskMapperTests.Map_InstallmentsSumExactlyToBoundPremium_WhenDivisionDoesNotRoundEvenly`
  and `Map_InstallmentsRemainEqual_WhenDivisionRoundsEvenly`.
