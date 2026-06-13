# PROGRESS.md

> **Multi-device continuity file.** This project is worked on from more than one machine (not at the
> same time). Read this first to see where things stand, then update the "Current status" and
> "Next steps" sections at the end of each working session so the next device/agent has context.
> For architecture and conventions, see [`AGENTS.md`](AGENTS.md).

_Last updated: 2026-06-13_

## Vision

A .NET 10 insurance ingest/transformation modular monolith that doubles as a **QA testing ground**
and is being grown into a **deployable product** with a Blazor UI, intended for deployment on a
Docker-capable VPS.

## Roadmap (phased)

- [x] **Phase 0 — AI-agnostic context + continuity.** `AGENTS.md`, `.github/copilot-instructions.md`,
  `.windsurf/rules/{architecture,testing}.md`, and this `PROGRESS.md`.
- [x] **Phase 1 — Document bugs.** `docs/project/KNOWN_ISSUES.md` cataloguing the verified issues found during
  analysis (documentation only; no code changes).
- [x] **Phase 2 — Per-line-of-business risk models.** `LineOfBusiness` enum + optional typed detail
  objects (Property/Cyber/Motor/Liability) on `CanonicalRiskRequest` + `IRiskTypeProfile` strategy
  per LOB replacing the `ProductCode.Contains(...)` checks in `RiskFlowService`. 12 new tests, 4
  example JSON files.
- [x] **Phase 3 — Blazor Server UI.** Interactive Server UI hosted inside
  `InsuranceIntegration.Api` (single host). Dashboard, Ingest form (envelope templates from the
  source-system catalog), Quotes/Policies list + detail views over snapshots, Domain-events log
  with filters, per-aggregate **Mermaid lifecycle flow diagram** from the event log, and a
  read-only **DB browser**. UI calls services via an `IUiGateway` facade that opens a fresh DI
  scope per operation (no long-lived circuit-scoped `DbContext`).
- [ ] **Phase 4 — Self-contained packaging.** Dockerfile + short VPS deployment guide.
  Dockerfile, `.dockerignore`, and `docs/guides/DEPLOYMENT.md` are written (2026-06-11) but the image
  build is **not yet verified** — Docker daemon was not running on the dev machine. Run
  `docker build -t insurance-integration .` to validate.
- [x] **Policy reinstatement (lifecycle gap closed).** `POST /api/v1/policies/reinstatements`
  restores a cancelled policy to in-force (status/phase `Reinstated`, `PolicyReinstated` event in
  one EF transaction). Added `ReinstatementRequest`/`ReinstatementResult`,
  `PolicyAdjustmentService.CalculateReinstatement` (lapse-day pro-rata math, optional admin fee,
  continuous-cover vs gap-in-cover handling), `IPolicyLifecycleService.ApplyReinstatement` (rejects
  non-cancelled policies), the `Reinstated` policy phase in `SnapshotMerge`, and the `Reinstated`
  stage in the UI lifecycle diagram. 6 new tests, 1 example JSON (2026-06-13). This was the last
  unimplemented item in the README "Target product direction" lifecycle list.
- [x] **Policy lapse + non-renewal (lifecycle).** `POST /api/v1/policies/lapses` lapses an in-force
  policy for non-payment (status/phase `Lapsed`, `PolicyLapsed` event) with pro-rata earned-premium
  and outstanding-shortfall math; `POST /api/v1/policies/non-renewals` marks an in-force policy
  not-renewed at expiry (status/phase `NonRenewed`, `PolicyNonRenewed` event). Added `Lapse`/
  `NonRenewal` transaction types, `Lapsed`/`NonRenewed` statuses + phases, `PolicyLapsed`/
  `PolicyNonRenewed` event types, `Lapse{Request,Result}`/`NonRenewal{Request,Result}`,
  `PolicyAdjustmentService.CalculateLapse`/`CalculateNonRenewal`, `IPolicyLifecycleService`
  `ApplyLapse`/`ApplyNonRenewal` (both reject non-in-force policies), the new phases in
  `SnapshotMerge` and the UI lifecycle diagram, and the event-type filter on the Events page.
  10 new tests (2026-06-13).
- [x] **Billing payment recording.** `POST /api/v1/billing/payments` applies a received payment to
  an installment schedule: it settles open installments (`Issued`/`Overdue` → `Paid`) in due order
  — optionally starting from a targeted installment number — then recomputes the billing position
  (outstanding balance, next due date, delinquency) via the existing `BillingFlowService`.
  Overpayment is surfaced as `UnappliedCredit`. Pure service (`IPaymentApplicationService` /
  `PaymentApplicationService`, no new entities/migrations — billing is computation-only) behind a
  thin endpoint. 8 new tests (2026-06-13).
- [x] **Billing delinquency / dunning.** `POST /api/v1/billing/delinquency` assesses an installment
  schedule as of a date (defaults to now, optional grace period): open installments whose due date
  has passed the grace cutoff transition `Issued`/`Planned` → `Overdue`, then the dunning and
  non-payment-cancellation recommendation is recomputed via `BillingFlowService` (≥1 overdue →
  dunning, ≥3 → `SeverelyDelinquent` + `PendingNonPaymentCancellation`). Pure service
  (`IDelinquencyAssessmentService` / `DelinquencyAssessmentService`, no new entities) behind a thin
  endpoint; pairs with the existing reinstatement flow to recover a lapsed/cancelled policy.
  8 new tests (2026-06-13).
- [x] **Claim status workflow.** `POST /api/v1/claims/transitions` validates a claim status move
  through the `Notified → Open → Reserved → Settled/Declined → Closed` state machine, rejecting
  illegal jumps and terminal-state moves, and requiring a reserve amount when reserving / a
  settlement amount when settling. Added `ClaimStatusValue`, the `Claim{Opened,Reserved,Settled,
  Declined,Closed}` domain-event types, and a pure `IClaimLifecycleService` /
  `ClaimLifecycleService` (claims remain computation-only — persisting the event log awaits the
  deferred claim snapshot). 19 new test cases (2026-06-13).
- [x] **Claim reserves & payments.** `POST /api/v1/claims/financials` applies a reserve or payment
  operation to a claim's financial position: `SetReserve` (absolute), `AdjustReserve` (signed
  delta, never below zero), `RecordIndemnityPayment` (adds to paid indemnity and draws the reserve
  down, floored at zero), and `RecordExpensePayment` (adds to paid expense, reserve untouched).
  Recomputes `incurred = paid indemnity + paid expense + outstanding reserve`. Pure service
  (`IClaimFinancialService` / `ClaimFinancialService`, no new entities). 10 new tests (2026-06-13).
- [x] **API-key authentication on writes + DB browser.** `ApiKeyAuthenticationMiddleware` (runs
  right after correlation-id handling) gates mutating requests (`POST`/`PUT`/`PATCH`/`DELETE`) and
  the `/database` browser behind an `X-Api-Key` header. Enforcement is **disabled by default** and
  turns on once keys are configured under the `ApiKey` section, so dev/test stay credential-free.
  Decision logic is isolated in a pure, unit-testable `ApiKeyValidator` (fixed-time key comparison);
  GET reads, `/health`, `/swagger`, OpenAPI, and static assets are never gated. 20 new test cases
  (2026-06-13).
- [x] **Policy schedule PDF.** `GET /api/v1/policies/{policyReference}/schedule.pdf` renders a
  one-page policy schedule (identifiers, parties, cover period, premium, coverage, history) from the
  `PolicySnapshot` using QuestPDF (free Community license, self-declared in `PolicyScheduleService`).
  Layout lives in `PolicyScheduleDocument`; a pure `IPolicyScheduleService` returns the PDF bytes,
  the endpoint serves `application/pdf` (404 when the snapshot is missing). 3 new tests (2026-06-13).
- [x] **UI lifecycle action forms.** The policy detail page (`/policies/{ref}`) now has a
  `LifecycleActions` panel to cancel / endorse / renew / reinstate / lapse / non-renew a policy
  directly from the Blazor UI (previously Swagger-only). Fields are pre-filled from the snapshot; the
  facade methods were added to `IUiGateway` / `UiGateway` (each opens its own DI scope and calls the
  existing `IPolicyLifecycleService` / `IPolicyRenewalService`), and a successful action reloads the
  snapshot in place. UI remains a thin facade over already-tested services, so no new unit tests
  (2026-06-13).
- [x] **Configurable outbox transport.** The `OutboxDispatcher` now publishes through a transport
  chosen by the `Outbox:Transport` setting: `Logging` (default, no infrastructure), `File`
  (`FileOutboxPublisher` appends each event as a JSON line to `Outbox:FilePath`), or `Webhook`
  (`WebhookOutboxPublisher` POSTs each event as JSON to `Outbox:WebhookUrl` using the built-in
  `HttpClient`). Selection is bound in `ServiceRegistration`; unknown/blank values fall back to
  `Logging` via `OutboxTransport.Normalize`. Failed deliveries throw so the dispatcher's existing
  retry/poison handling applies. Broker adapters (RabbitMQ / Azure Service Bus) still need their own
  packages and are out of scope here. 19 new tests (transport selection, envelope projection, file
  and webhook publishers) — 241 total (2026-06-13).
- [x] **Bug-fix pass (was "fix in a later separate pass").** H1 (outbox never published — new
  `IOutboxPublisher` + retry/poison handling), H2 (idempotency TOCTOU — atomic insert-first,
  first-writer-wins), H3 (negative renewal premium now throws instead of clamping to 0), and
  M2 (installment rounding residual goes to the last installment). 6 new regression tests;
  details moved to the "Fixed" section of `docs/project/KNOWN_ISSUES.md`.

## Current status

- Build green (`dotnet build -c Release`); **all 241 tests pass** (configurable outbox transport
  added +19 and policy schedule PDF added +3 and
  API-key auth added +20 on
  2026-06-13; claim reserves/payments added +10
  on 2026-06-13; claim status workflow added +19; billing delinquency/dunning added +8; billing
  payment recording added +8; lifecycle lapse/non-renewal added +10; earlier passes added
  reinstatement and the M1/M3/M4 fixes). UI added in
  Phase 3 introduces no new warnings and no new tests (UI is a thin facade over already-tested services).
- NuGet packages bumped to latest (2026-06-11): EF Core / AspNetCore.OpenApi 10.0.9,
  Swashbuckle.SwaggerUI 10.2.1, NUnit 4.6.1, NUnit3TestAdapter 6.2.0, Test.Sdk 18.6.0,
  TimeProvider.Testing 10.7.0, coverlet 10.0.1. Build + tests verified after the bump.
  Added `.github/dependabot.yml` (weekly NuGet, monthly Actions, minor+patch grouped) so the
  10.0.x LTS patch train keeps flowing without manual checks; CI validates every Dependabot PR.
- `dotnet format --verify-no-changes --severity warn` is clean (exit 0); all new UI files are
  format-clean.
- Smoke-tested: app boots in Development, `/`, `/ingest`, `/quotes`, `/policies`, `/events`,
  `/database`, `/health`, and `/app.css` all return 200.
- Test framework: **NUnit 4** (+ `FakeTimeProvider`).
- **Docs reorganized (2026-06-13):** all documentation grouped under `docs/{guides,reference,project}/`
  with a new `docs/README.md` index as the entry point; source-path breadcrumbs in the guides
  normalized to repository-root-relative form (`src/...`, `tests/...`). Documentation-only change —
  no code/build/test impact.
- **Working on now:** Phase 4 wrap-up — verify the Docker image builds and runs (`docker build`,
  then the run command in `docs/guides/DEPLOYMENT.md`); Docker daemon was unavailable when the
  Dockerfile was authored.

### Phase 3 UI map (where things live)

- UI host wiring: `Program.cs` (`AddRazorComponents().AddInteractiveServerComponents()`,
  `UseStaticFiles`/`UseAntiforgery`, `MapRazorComponents<App>().AddInteractiveServerRenderMode()`).
- Facade: `Services/Ui/IUiGateway.cs` + `UiGateway.cs` (registered singleton; one DI scope per
  call). DB browser uses `sqlite_master` + table-name validation against the live schema before
  interpolating the (non-parameterizable) identifier; `LIMIT`/`OFFSET` are parameterized.
- Mermaid: `Services/Ui/EventFlowDiagram.cs` builds the flowchart; `wwwroot/js/app.js`
  (`mermaidInterop.render`) renders it via the locally-vendored `wwwroot/js/mermaid.min.js`
  (pinned `mermaid@11.6.0`, UMD build exposing `globalThis.mermaid`), referenced from
  `Components/App.razor`. No CDN/network dependency at runtime.
- Components: `Components/{App,Routes,_Imports}.razor`, `Components/Layout/*`,
  `Components/Shared/EventFlow.razor`, `Components/Pages/{Home,Ingest,Quotes,QuoteDetail,Policies,
  PolicyDetail,Events,Database}.razor`. Styles in `wwwroot/app.css`.
- **DB browser has no auth** (per locked-in decision) — gate behind Development / a feature flag
  before any real production exposure. Mermaid is vendored locally (`wwwroot/js/mermaid.min.js`),
  so the UI renders diagrams fully offline.

## Decisions locked in

- **Stay on .NET.** No language migration. UI is **Blazor Server** (single language, single host,
  self-contained) — not React/Vue, to minimise toolchains.
- **Bugs:** document now (Phase 1), fix in a later separate pass.
- **Risk models:** lightweight (additive typed detail objects + strategy), not a subclass hierarchy.
- **DB browser:** read-only, available in all environments, no auth yet (revisit before real
  production exposure — recommend gating behind Development or a feature flag).
- **Hosting target:** a Docker-capable Linux **VPS** (root access required for Docker; shared/cPanel
  hosting cannot run the app).
- **No language/stack migration (hosting analysis).** Hosting cost is driven by the VPS, not the
  language: a Linux VPS that runs Node/Python also runs .NET for the same price. A JS/TS or Blazor
  WASM static frontend can be hosted free, but the backend (background outbox dispatcher, EF +
  SQLite, persistent connections) still needs the same always-on VPS, so total cost is unchanged.
  Avoiding the VPS entirely requires re-architecting to serverless + a hosted DB (works in .NET
  too, usually costs *more*); the only genuinely-free no-VPS path is Cloudflare Workers + D1, which
  is JS-native but would mean discarding the whole architecture/tests/UI. Conclusion: **keep .NET +
  Blazor Server**; minimise cost via a small trimmed container (Phase 4), not a rewrite.

## Next steps

1. Verify Phase 4: build the image (`docker build -t insurance-integration .`), run it with the
   `/data` volume per `docs/guides/DEPLOYMENT.md`, and confirm `/health` + UI work in the container.
2. Gate the read-only DB browser behind Development or a feature flag before production exposure
   (interim: proxy-level basic auth / IP allowlist on `/database`, as noted in `docs/guides/DEPLOYMENT.md`).
3. Remaining known issues: M1 (billing fallback due date), M3 (hard-coded underwriting
   thresholds), M4 (`DateTime.UtcNow` in mappers/handlers — inject `TimeProvider`), M5 (snapshot
   premium not cleared on explicit zero). See `docs/project/KNOWN_ISSUES.md`.
4. UI polish (optional): add submission/quote/policy lifecycle action forms
   (cancel/endorse/renew/reinstate) that post to the existing `Endpoints/PolicyEndpoints.cs`
   routes; today those are reachable via Swagger only.
5. Feature backlog: a full enumeration of candidate functionality (with add/defer/skip verdicts)
   lives in `docs/project/FEATURE_PLAN.html` — open it in a browser to pick the next feature.

## Open questions / to confirm with hosting provider

- VPS provider: which Linux OS options, and is Docker installable out of the box? (Confirm with the
  chosen provider; root access is typically required.)
