# Known issues

Catalogue of bugs and business-logic concerns found during code analysis. **These are documented
here intentionally and are NOT yet fixed** - fixing is a separate, later pass (see `PROGRESS.md`).
Some odd logic may be deliberate "bait" for the QA testing-ground purpose; those are flagged.

Each item lists: location, what the code does, why it's a problem, severity, and a suggested fix.
Line numbers were verified against the codebase on 2026-06-03 but may drift as the code changes -
treat them as a starting point and confirm before fixing.

Legend: 🔴 High · 🟠 Medium · 🟡 Low · 🔵 Debatable / possibly by-design · ✅ Fixed

---

## 🔴 High

_None open. H1-H3 were fixed on 2026-06-11 - see the Fixed section below._

---

## 🟠 Medium

_None open. M1-M5 have been fixed - see the Fixed section below._

---

## 🟡 Low

### L1 - Contoso mapper hard-codes insured/broker data
- **Where:** `Mappers/Risks/ContosoRiskMapper.cs`.
- **What:** Broker name, insured revenue, employee count, and years-in-business are hard-coded
  constants; only insured name and estimated premium come from the payload.
- **Why it matters:** Any real values the source provides would be discarded; underwriting signals
  could be based on fabricated data. (May be acceptable as a demo/QA fixture.)
- **Suggested fix:** Map real fields from the source payload; keep constants only as explicit
  defaults where the source genuinely omits data.

### L2 - Non-deterministic `Guid.NewGuid()` for entity ids in mappers/handlers
- **Where:** `Mappers/Risks/*RiskMapper.cs` and several ingest handlers.
- **What:** `EntityId` is generated with `Guid.NewGuid()` inside mapping logic.
- **Why it matters:** Harder to assert in tests and to correlate across a pipeline run. Minor.
- **Suggested fix:** Generate ids at a single boundary, or inject an id/Guid provider for
  determinism in tests.

---

## 🔵 Debatable / possibly by-design

### D1 - BindPoint installment due dates start at inception
- **Where:** `Mappers/Risks/BindPointRiskMapper.cs` - `DueDate = InceptionDate.AddMonths(index - 1)`
  for `index` in `1..InstallmentCount`.
- **Note:** The initial analysis called this an off-by-one (last payment one month "early"). But a
  "pay first installment at inception, then monthly" schedule is a legitimate billing convention
  (12 installments from a Jan inception fall Jan-Dec). **Confirm the intended convention** before
  changing - it may be correct as-is.

### D2 - Pre-existing build warnings (38)
- **What:** `dotnet build` produces 38 analyzer/style warnings (nullable assignments, `CA1869`
  cached `JsonSerializerOptions`, `CA1707` migration naming, etc.). Build still succeeds.
- **Note:** Not bugs per se, but they erode the "no new warnings" bar. Worth a cleanup pass;
  migration-file warnings can be suppressed for generated code.

---

_When an item here is fixed, remove it (or move it to a "Fixed" section) and add a regression test._

---

## ✅ Fixed

### M5 - Snapshot premium not cleared when an update sets premium to zero _(fixed 2026-06-14)_
- The policy and quote snapshot projectors previously guarded premium updates with `> 0m`, which
  conflated "no premium provided" with a legitimate zero. They now apply the premium when the
  resolved premium is positive **or** the request explicitly provided a premium input - the latter
  via a shared `SnapshotMerge.PremiumProvided(request)` signal that mirrors
  `RiskFlowService.ResolveBasePremium`'s nullable inputs (`Submission.BrokerPremium`,
  `Submission.TechnicalPremium`, `AnnualizedGrossPremium`). An explicit zero (e.g. a waived policy)
  now clears the stale snapshot value, while an absent premium still preserves the existing one.
- Regression test: `tests/.../Snapshots/PolicySnapshotProjectorTests.cs`
  (`Apply_AppliesExplicitZeroPremium_WhenTransactionProvidesZeroPremium`); the existing
  `Apply_MergesSubsequentEventPreservingExistingFields` covers the absent-premium preserve case.

### M1 - Billing next-due-date derived from missed-payment count (no-schedule path) _(fixed 2026-06-13)_
- `BillingFlowService` now derives the no-schedule next due date from the billing frequency implied
  by the installment count (12 → monthly, 4 → quarterly, 2 → semi-annual, 1 → annual), advanced by
  the number of settled installments - not the missed-payment count. Fully-settled schedules return
  no next due date, matching the explicit-schedule path.
- Regression tests: `tests/.../Flows/BillingFlowServiceTests.cs`
  (`Process_DerivesQuarterlyNextDueDateFromBillingFrequency_WhenScheduleNotProvided`,
  `Process_DerivesMonthlyNextDueDateFromBillingFrequency_WhenScheduleNotProvided`).

### M3 - Hard-coded, inconsistent underwriting thresholds _(fixed 2026-06-13)_
- The decline (25,000) and auto-clear (5,000) incurred-loss thresholds moved out of
  `RiskFlowService` into `ClearanceData` (`DeclineIncurredThreshold`, `AutoClearIncurredThreshold`),
  mirroring the existing `PremiumThreshold`. Defaults preserve prior behavior; both are now
  overridable per request/product.
- Regression tests: `tests/.../Flows/RiskFlowServiceTests.cs`
  (`Process_DeclineIncurredThreshold_IsConfigurable`, `Process_AutoClearIncurredThreshold_IsConfigurable`).

### M4 - `DateTime.UtcNow` used directly in logic (violates `TimeProvider` convention) _(fixed 2026-06-13)_
- `BindPointRiskMapper`, `ContosoRiskMapper`, and `QuoteForgeRiskMapper` now take an injected
  `TimeProvider`; `RiskFlowService` takes an (optional) `TimeProvider` and uses it for the
  year-boundary enrichment check. The ingest handlers already received a `TimeProvider`. (The
  Blazor `Ingest.razor` demo page still stamps `occurredAtUtc` with wall-clock time - that is
  presentation-layer sample data, not business logic.)
- Regression tests: mapper tests pin a `FakeTimeProvider` and assert deterministic
  `TransactionTimestampUtc` (`tests/.../Mappers/Risks/*RiskMapperTests.cs`).

### H1 - Outbox messages were marked dispatched but never actually sent _(fixed 2026-06-11)_
- Introduced `IOutboxPublisher` (default `LoggingOutboxPublisher`; the transport is now selectable
  via `Outbox:Transport` = `Logging` / `File` / `Webhook`). `OutboxDispatcher` now only sets
  `DispatchedAtUtc` after a successful publish;
  on failure it records `LastError`, leaves the message pending, and retries on later polls up to
  `MaxDispatchAttempts` (5), after which the message is poisoned and skipped.
- Regression tests: `tests/.../Outbox/OutboxDispatcherTests.cs` (publish success, failure leaves
  pending, attempt cap/poison, recovery after transient failure).

### H2 - Idempotency check was not atomic (TOCTOU race) _(fixed 2026-06-11)_
- `EfCoreIdempotencyStore.StoreAsync` now inserts first and treats the composite-key
  `(Source, EnvelopeId)` unique-constraint violation as "already processed" - first writer wins
  and its receipt is returned as the canonical outcome. `IIdempotencyStore.StoreAsync` returns the
  winning receipt; `IngestDispatcher` returns it to the caller.
- Regression test: `EfCoreIdempotencyStoreTests.StoreAsync_FirstWriterWins_OnConcurrentInsertForSameKey`.

### H3 - Renewal premium silently clamped to zero _(fixed 2026-06-11)_
- `PolicyRenewalService` now throws `ArgumentException` (→ 400 with the load breakdown in the
  message) when the computed renewal premium is negative, instead of silently producing a $0
  premium.
- Regression test: `PolicyRenewalServiceTests.ApplyRenewal_ThrowsWhenComputedRenewalPremiumIsNegative`.

### M2 - Installment rounding gap (totals didn't reconcile) _(fixed 2026-06-11)_
- `BindPointRiskMapper` now adds the rounding residual to the **last** installment so the schedule
  always sums exactly to `BoundPremium` (e.g. 1000 / 3 → 333.33 + 333.33 + 333.34).
- Regression tests: `BindPointRiskMapperTests.Map_InstallmentsSumExactlyToBoundPremium_WhenDivisionDoesNotRoundEvenly`
  and `Map_InstallmentsRemainEqual_WhenDivisionRoundsEvenly`.
