# Known issues

Catalogue of bugs and business-logic concerns found during code analysis. **These are documented
here intentionally and are NOT yet fixed** — fixing is a separate, later pass (see `PROGRESS.md`).
Some odd logic may be deliberate "bait" for the QA testing-ground purpose; those are flagged.

Each item lists: location, what the code does, why it's a problem, severity, and a suggested fix.
Line numbers were verified against the codebase on 2026-06-03 but may drift as the code changes —
treat them as a starting point and confirm before fixing.

Legend: 🔴 High · 🟠 Medium · 🟡 Low · 🔵 Debatable / possibly by-design

---

## 🔴 High

### H1 — Outbox messages are marked dispatched but never actually sent
- **Where:** `src/InsuranceIntegration.Api/Services/Outbox/OutboxDispatcher.cs` — `DispatchBatchAsync`
  (around lines 70–87).
- **What:** For each pending message it logs, then sets `DispatchedAtUtc`, increments
  `DispatchAttempts`, and `SaveChangesAsync`. **No message is ever sent to any destination.**
- **Why it matters:** The transactional-outbox pattern is the platform's mechanism for emitting
  events to downstream consumers. Messages are silently consumed (marked done) without delivery, so
  no external system ever receives policy-bound/claim/etc. events. There is also no retry/poison
  handling since success is assumed.
- **Suggested fix:** Introduce an `IOutboxPublisher` abstraction; only set `DispatchedAtUtc` after a
  successful publish; on failure set `LastError`, leave `DispatchedAtUtc` null, and cap attempts.

### H2 — Idempotency check is not atomic (TOCTOU race)
- **Where:** `Services/Ingest/IngestDispatcher.cs` (check-then-process) and
  `Services/Ingest/EfCoreIdempotencyStore.cs` (`StoreAsync` re-query then insert/update).
- **What:** Existence is checked, then processing/insertion happens as a separate step. Two
  concurrent identical envelopes can both pass the check and both process/insert.
- **Why it matters:** Duplicate ingest can create duplicate downstream effects (policies, claims,
  billing). The intended idempotency guarantee is not enforced atomically.
- **Suggested fix:** Rely on the DB unique key (composite `(Source, EnvelopeId)`): attempt the
  insert first and catch the unique-constraint violation as "already processed", or wrap
  check+write in a transaction with appropriate isolation.

### H3 — Renewal premium silently clamped to zero
- **Where:** `Services/Policies/PolicyRenewalService.cs`, lines ~55–58.
- **What:** `renewalPremium = Round(PriorAnnualPremium * (1 + totalLoad))`; if the result is `< 0`
  it is silently set to `0`.
- **Why it matters:** A very negative `totalLoad` (bad data or a calculation error) produces a $0
  renewal premium with no signal. That's revenue loss masking an underlying problem.
- **Suggested fix:** Treat a computed negative premium as an error/needs-review outcome (reason +
  status), or floor at a configured minimum premium rather than silently at zero.

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

### M2 — Installment rounding gap (totals don't reconcile)
- **Where:** `Mappers/Risks/BindPointRiskMapper.cs`, lines ~24–38.
- **What:** `installmentAmount = Round(BoundPremium / InstallmentCount, 2)` is applied to *every*
  installment. The sum of equal rounded installments can differ from `BoundPremium`
  (e.g. 1000 / 3 = 333.33 × 3 = 999.99).
- **Why it matters:** Billing totals won't reconcile to the bound premium; small but real disputes
  and reconciliation noise.
- **Suggested fix:** Distribute the rounding remainder (e.g. add the residual cent(s) to the
  first or last installment) so installments sum exactly to the premium.

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
