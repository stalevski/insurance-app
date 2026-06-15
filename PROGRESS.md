# PROGRESS.md

> **Multi-device continuity file.** This project is worked on from more than one machine (not at the
> same time). Read this first to see where things stand, then update the "Current status" and
> "Next steps" sections at the end of each working session so the next device/agent has context.
> For architecture and conventions, see [`AGENTS.md`](AGENTS.md).

_Last updated: 2026-06-15_

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
- [x] **Phase 4 — Self-contained packaging.** Dockerfile + short VPS deployment guide.
  Dockerfile, `.dockerignore`, and `docs/guides/DEPLOYMENT.md` (2026-06-11). **Verified end-to-end
  on 2026-06-14** with Docker 29.5.3: `docker build -t insurance-integration .` completes the
  multi-stage restore→publish→runtime build (~46s), and `docker run -d -p 8080:8080 -v
  insurance-data:/data` (per `docs/guides/DEPLOYMENT.md`) starts a container that reports
  **`Up (healthy)`** (the `/health` HEALTHCHECK passes). Confirmed from the host: `GET /health` →
  200 (`{"status":"Healthy",... ".NET 10.0.9"}`) and the Blazor UI `/` → 200
  (`<title>Insurance Integration</title>`). Production hardening confirmed: `/swagger` → 404
  (disabled outside Development), the process runs as the **non-root** user (UID 1654), and the
  SQLite database is written to the mounted volume at `/data/integration.db` (migrations applied on
  startup). Remaining is real-world deployment only (provision a VPS + TLS reverse proxy per the
  guide).
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
- [x] **M5 snapshot zero-premium fix.** The policy and quote snapshot projectors no longer guard
  premium updates with `> 0m` (which silently ignored a legitimate zero, e.g. a waived policy).
  They now apply the premium when the resolved premium is positive or the request explicitly
  provided a premium input — the latter via a shared `SnapshotMerge.PremiumProvided(request)` signal
  mirroring `RiskFlowService.ResolveBasePremium`'s nullable inputs (`Submission.BrokerPremium`,
  `Submission.TechnicalPremium`, `AnnualizedGrossPremium`) — so an explicit zero clears the stale
  snapshot value while an absent premium preserves it. 1 new regression test; details in the
  "Fixed" section of `docs/project/KNOWN_ISSUES.md` (2026-06-14).
- [x] **DB browser environment gate (pre-prod hardening).** The read-only `/database` browser is
  now gated by a pure, unit-tested `DatabaseBrowserGate`: enabled only in the Development environment
  by default, overridable via the `DatabaseBrowser` config section (`DatabaseBrowser__Enabled=true`
  to force on, `false` to force off). A `DatabaseBrowserGateMiddleware` (runs right after the API-key
  middleware) returns **404** for `/database` when disabled — 404 rather than 403 so the page's
  existence is not revealed — and the nav link + page render conditionally on the same gate (defence
  in depth). Follows the existing `ApiKeyOptions`/`ApiKeyValidator` pattern. Closes "Next steps #2".
  15 new tests (2026-06-14).- [x] **README human-friendly rewrite + UI screenshots, and 1-based list pagers.** Added a
  plain-language intro to `README.md` (what the platform does, who it's for, a Mermaid pipeline
  diagram), a **"Visual tour"** section embedding seven captured UI screenshots under
  `docs/screenshots/` (dashboard, ingest, quotes, policies, policy detail, events, DB browser), and
  a "Run it locally" quick-start — all preserving the existing technical depth. Screenshots are
  captured headlessly with Playwright at 1440×1000 (deviceScaleFactor 2) against the seeded
  Development data. Also made the Quotes/Policies/Events list pagers 1-based ("Showing 1–9" not
  "0–9"), matching the earlier Database-page fix. Added `scripts/start.ps1` / `scripts/stop.ps1`
  dev helpers (start/stop the app, free port 5000) and matching VS Code "Start app" / "Stop app"
  tasks (`.vscode/tasks.json` un-ignored in `.gitignore` so it is shared). Docs + Razor UI + dev
  tooling only; no new tests (still 257) (2026-06-14).
- [x] **Dark-mode UI screenshots + theme-aware Mermaid diagrams.** Recaptured all seven
  `docs/screenshots/01..07` in dark mode, and made the Mermaid lifecycle diagrams theme-aware so
  their canvas matches the dark panel instead of rendering a bright white rectangle. `mermaidInterop`
  (`wwwroot/js/app.js`) now picks Mermaid's `dark`/`neutral` theme from the document `data-theme`,
  sets `themeVariables.background` to the computed `--panel-muted` surface, caches each rendered
  diagram, and re-renders them all when the theme toggles; the `.mermaid-diagram` dark-mode
  background override was removed from `app.css`, and `EventFlowDiagram` gives the highlighted
  "current" node explicit dark text so it stays legible. Static assets + one diagram-builder string
  only; no new tests (still 257) (2026-06-14).
- [x] **README reframed to lead with QA-testing-ground purpose + test-gap audit.** Rewrote the
  `README.md` opening, "Who is it for?", and "Project goal", and added a top-level **Testing**
  section, so the README leads with the project's primary purpose — a realistic system under test
  with a REST API and a Blazor UI — instead of reading as a backend-only platform; the GitHub About
  was updated to match. Ran a full coverage audit (suite green at **257 tests**) and captured the
  gaps — zero HTTP-endpoint tests, zero Blazor-UI tests, and six untested service modules — as the
  new "Test coverage backlog" section below. Docs only; no code or test changes (still 257)
  (2026-06-15).
## Current status

- Build green (`dotnet build -c Release`); **all 257 tests pass** (DB browser environment gate added
  +15 and M5 snapshot zero-premium fix added +1 on 2026-06-14; configurable outbox transport
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
- **Working on now:** **Dark-mode UI polish (2026-06-14)** — recaptured the seven Visual-tour
  screenshots (`docs/screenshots/01..07`) in dark mode and made the Mermaid lifecycle diagrams
  theme-aware (dark canvas matching `--panel-muted`, re-render on theme toggle) so they no longer
  show a white rectangle in dark mode. Preceded by the **README human-friendly rewrite + UI
  screenshots** — `README.md` now opens with a plain-language explanation, a Mermaid pipeline
  diagram, a "Run it locally" quick-start, and a **"Visual tour"** embedding the seven UI
  screenshots, while keeping all the existing technical sections. The Quotes/Policies/Events list
  pagers were also made 1-based to match the Database page. Earlier on 2026-06-14: the **DB browser environment gate**
  — the read-only `/database` browser is now gated by `DatabaseBrowserGate` +
  `DatabaseBrowserGateMiddleware` (off → 404 outside Development unless `DatabaseBrowser__Enabled=true`);
  the nav link and page render on the same gate. This closes the pre-production "Next steps #2"
  hardening item. Also on 2026-06-14: Phase 4 was
  **verified end-to-end** (Docker daemon finally available — 29.5.3) — the image builds, the
  container runs **healthy** on port 8080, host `/health` → 200 and the Blazor UI `/` → 200,
  Production hardening confirmed (`/swagger` 404, non-root UID 1654, SQLite on the `/data` volume);
  and M5 (snapshot zero-premium) was fixed — the last documented High/Medium bug is now cleared. All
  four roadmap phases are complete; remaining work is real-world VPS deployment (provision host + TLS
  reverse proxy per `docs/guides/DEPLOYMENT.md`) and the optional/backlog items below.

### Phase 3 UI map (where things live)

- UI host wiring: `Program.cs` (`AddRazorComponents().AddInteractiveServerComponents()`,
  `UseStaticFiles`/`UseAntiforgery`, `MapRazorComponents<App>().AddInteractiveServerRenderMode()`).
- Facade: `Services/Ui/IUiGateway.cs` + `UiGateway.cs` (registered singleton; one DI scope per
  call). DB browser uses `sqlite_master` + table-name validation against the live schema before
  interpolating the (non-parameterizable) identifier; `LIMIT`/`OFFSET` are parameterized.
- Mermaid: `Services/Ui/EventFlowDiagram.cs` builds the flowchart; `wwwroot/js/app.js`
  (`mermaidInterop.render`) renders it via the locally-vendored `wwwroot/js/mermaid.min.js`
  (pinned `mermaid@11.6.0`, UMD build exposing `globalThis.mermaid`), referenced from
  `Components/App.razor`. No CDN/network dependency at runtime. `mermaidInterop` is **theme-aware**:
  it initializes Mermaid with the `dark`/`neutral` theme from the document `data-theme`, sets
  `themeVariables.background` to the computed `--panel-muted` surface, caches each diagram, and
  `themeInterop.set` calls `mermaidInterop.rerenderAll()` so diagrams re-render on a light/dark
  toggle.
- Components: `Components/{App,Routes,_Imports}.razor`, `Components/Layout/*`,
  `Components/Shared/EventFlow.razor`, `Components/Pages/{Home,Ingest,Quotes,QuoteDetail,Policies,
  PolicyDetail,Events,Database}.razor`. Styles in `wwwroot/app.css`.
- **DB browser is environment-gated** by `DatabaseBrowserGate` + `DatabaseBrowserGateMiddleware`:
  on in Development, off (returns 404) elsewhere unless `DatabaseBrowser__Enabled=true`. The nav link
  and page render only when the gate is enabled. Mermaid is vendored locally
  (`wwwroot/js/mermaid.min.js`), so the UI renders diagrams fully offline.

## Decisions locked in

- **Stay on .NET.** No language migration. UI is **Blazor Server** (single language, single host,
  self-contained) — not React/Vue, to minimise toolchains.
- **Bugs:** document now (Phase 1), fix in a later separate pass.
- **Risk models:** lightweight (additive typed detail objects + strategy), not a subclass hierarchy.
- **DB browser:** read-only; **gated by environment** — available in Development, returns 404
  elsewhere unless explicitly enabled via the `DatabaseBrowser:Enabled` flag
  (`DatabaseBrowser__Enabled=true`). Even when exposed, front it with proxy auth / an IP allowlist.
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

1. Phase 4 image build/run is **verified** (2026-06-14, see Current status). Remaining is
   real-world deployment only: provision a Docker-capable Linux VPS, then build + run per
   `docs/guides/DEPLOYMENT.md` behind a TLS reverse proxy (Caddy/Nginx).
2. ~~Gate the read-only DB browser behind Development or a feature flag before production exposure.~~
   **Done (2026-06-14):** `DatabaseBrowserGate` + `DatabaseBrowserGateMiddleware` disable `/database`
   (404) outside Development unless `DatabaseBrowser__Enabled=true`. For deliberate exposure, still
   add proxy-level basic auth / an IP allowlist on `/database` (see `docs/guides/DEPLOYMENT.md`).
3. Remaining known issues: all High and Medium items are fixed; only low/debatable ones remain —
   L1 (Contoso mapper hard-codes insured/broker data), L2 (`Guid.NewGuid()` entity ids in
   mappers/handlers), D1 (BindPoint installment due dates — confirm the intended convention), and
   D2 (38 pre-existing build warnings). See `docs/project/KNOWN_ISSUES.md`.
4. UI polish (optional): add submission/quote/policy lifecycle action forms
   (cancel/endorse/renew/reinstate) that post to the existing `Endpoints/PolicyEndpoints.cs`
   routes; today those are reachable via Swagger only.
5. Feature backlog: a full enumeration of candidate functionality (with add/defer/skip verdicts)
   lives in `docs/project/FEATURE_PLAN.html` — open it in a browser to pick the next feature.
6. Test refactoring (tech debt): the test suite likely needs an **expected-results builder** to
   construct the expected canonical/response objects (and request fixtures) instead of hand-building
   them inline in each test. Related: **review the test classes for code duplication** — repeated
   arrange/setup (e.g. building requests, `FakeTimeProvider` wiring, in-memory SQLite/`DbContext`
   setup, common assertions) should be extracted into private helper methods within a test class, or
   a shared **base test class** / fixtures where the duplication spans multiple classes.

## Test coverage backlog (gaps to fill, audited 2026-06-15)

Current suite: **257 NUnit 4 tests, all green** — strong at the service / flow / mapper / snapshot /
outbox / security layers (unit + in-memory-SQLite integration). Since QA is this project's primary
purpose, the coverage gaps below are first-class backlog items. A 2026-06-15 audit found **zero
HTTP-endpoint tests and zero Blazor-UI tests**, plus six service modules with no direct unit tests.
Listed most-valuable first:

1. **HTTP endpoint integration tests** (biggest gap). Stand up the API in-process with
   `WebApplicationFactory<Program>` (the API uses top-level `Program.cs` — expose it with
   `public partial class Program { }`) and assert status codes, JSON bodies, and headers for the
   ~29 routes across `Endpoints/*.cs`. One happy-path test per route first, then the error paths.
2. **HTTP error / validation paths** — 400 (missing/invalid fields), 404 (unknown policy / quote /
   envelope), and 401 (API-key enforcement on writes + the `/database` browser) per endpoint family.
3. **Middleware pipeline at the HTTP layer** — correlation-id echo (`CorrelationIdMiddleware`),
   `X-Api-Key` accept/reject (`ApiKeyAuthenticationMiddleware`), and the `/database` 404 gate
   (`DatabaseBrowserGateMiddleware`) exercised through real requests, not just their pure validators.
4. **Untested service modules** (no direct unit tests today): `Services/Events/DomainEventLog`
   (filtering + ordering + same-transaction writes), `Services/Schemas/JsonSchemaService` (schema
   shape per contract), `Services/Catalog/SourceSystemCatalogService`, `Services/Products/ProductCatalog`
   (lookups + rating defaults), `Services/Seeding/DevelopmentDataSeeder` (idempotency / no-op when
   data already exists), and the `Services/Ui/*` facade (`UiGateway` aggregation + paging,
   `EventFlowDiagram` / lifecycle-stage Mermaid output).
5. **Blazor UI component tests** with **bUnit** — render `Home`, `Ingest`, `Quotes`, `Policies`,
   `PolicyDetail`, `Events`, `Database` against a stub `IUiGateway`; assert rendered rows, 1-based
   pager state, filter behaviour, and the lifecycle-actions panel.
6. **End-to-end UI smoke** with **Playwright** — drive quote → bind → policy → claim → billing
   through the running app and assert the UI + the `/database` browser reflect the change (mirrors
   the Playwright tooling already used for the screenshot capture).
7. **Concurrency / race tests** — idempotency store first-writer-wins under parallel ingest, outbox
   dispatch under contention, and snapshot rebuild while events are appended.

> Approach note: prefer the existing in-memory-SQLite + `WebApplicationFactory` stack (no Docker
> needed for the API tests); keep bUnit / Playwright additive so `dotnet test` stays fast and
> dependency-light. Record the new test totals here as each slice lands.

## Enterprise / multi-tenant backlog (candidate epic — design first, not yet scheduled)

Captured from an enterprise expansion brief. These are **large, cross-cutting** changes; each needs
its own design pass + KNOWN_ISSUES/FEATURE_PLAN entry before implementation. Listed roughly in
dependency order:

1. **Multi-tenant data isolation.** Introduce a `TenantId` primitive (or elevate `SourceSystem`).
   Add a request-scoped `ITenantProvider` resolving the tenant from the API key / a JWT claim / an
   HTTP header, and enforce isolation via EF Core **global query filters** in `IntegrationDbContext`
   so every read/update/snapshot query is tenant-scoped. Update `DevelopmentDataSeeder` to assign
   tenants. *Risk:* query filters interact with the existing `RowVersionInterceptor` and the
   same-transaction event+snapshot invariant — needs careful testing. Migration adds a column to
   `IngestEntries`, `DomainEvents`, `PolicySnapshots`, `QuoteSnapshots`.
2. **Dynamic per-product JSON Schema validation.** Replace compile-time contract annotations with a
   config/DB-backed lookup keyed by `ProductCode` / line of business (Property, Cyber, Motor,
   Liability). Add a pre-ingest pipeline step that validates raw source envelopes against the
   runtime schema *before* mapping to canonical contracts. We already vendor a JSON Schema stack
   (`IJsonSchemaService`) — evaluate reusing it before adding `JsonSchema.Net` (respect the
   "no phantom dependencies" rule).
3. **Aggregate version sequencing (stronger concurrency control).** Add an explicit
   `AggregateVersion` integer to `DomainEvents` and the snapshot documents, and enforce a
   sequence check in the snapshot projector: abort unless the incoming event targets
   `CurrentSnapshot.Version + 1`. Complements (does not replace) the existing `RowVersion`
   optimistic concurrency. Closes the high-volume read-modify-write race (concurrent broker
   adjustments to the same policy reference).
4. **Outbox DLQ operational UI + manual re-drive.** Add a thin read-only endpoint + Blazor page
   listing pending / failed / poisoned outbox messages (the dispatcher already tracks
   `DispatchAttempts`, `LastError`, and the poison cap), plus a guarded admin action to invoke a
   targeted `OutboxDispatcher` retry cycle for an individual message. Builds directly on the
   configurable transport just shipped (`Logging` / `File` / `Webhook`).
5. **System branding/rename (decision required, do NOT action the rest yet).** The enterprise brief
   proposed renaming the source-system placeholders. The first source-system placeholder has already
   been actioned — it was renamed to **Contoso** (a deliberately-synthetic placeholder) because it
   was uncertain whether the original echoed a real system; this touched `SourceContracts`,
   `Mappers`, the source-system catalog (`CONTOSO_UW`), sample payloads, the Postman collection,
   docs, and the tests. The other two (`QuoteForge`, `BindPoint`) are **left as-is**; the brief's
   proposed names (`SlipStream`, `Bedrock`) are not adopted. Treat any further rename as a dedicated
   mechanical pass only after the new names are confirmed final and original.

> Architectural guards to preserve across all of the above: thin endpoints; strict
> `SourceContracts → Mappers → CanonicalContracts → Responses` separation; one public type per file
> with namespace matching folder; inject `TimeProvider` (no `DateTime.UtcNow` in logic); `decimal`
> for money; all mutating writes flow through `ApiKeyAuthenticationMiddleware` (`X-Api-Key`) and
> integration recipes must supply that header; no new heavy dependencies (AutoMapper/MediatR) — use
> plain C# orchestration + the existing configuration extensions.

## Open questions / to confirm with hosting provider

- VPS provider: which Linux OS options, and is Docker installable out of the box? (Confirm with the
  chosen provider; root access is typically required.)
