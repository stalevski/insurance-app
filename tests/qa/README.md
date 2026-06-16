# Insurance App — QA automation (Playwright)

Black-box quality automation for the **InsuranceIntegration** app (ASP.NET Core minimal
API + Blazor Server UI on a single host). This suite treats the app as a deployed system:
it drives the public HTTP API, the Blazor UI, and accessibility checks — with **no access
to application internals**.

> White-box developer tests (NUnit unit + in-process integration) live separately under
> [`tests/dev/`](../dev). This `tests/qa/` project is the black-box tier.

## Layout

```
tests/qa/
  playwright.config.ts        # projects, single-worker run, webServer (starts the app)
  test-targets.config.ts      # base URLs (UI + API share one origin)
  src/
    core/                     # framework-agnostic base classes
      api/base-api.client.ts  #   BaseApiClient (get/post/put/patch/delete + expectOk)
      ui/base.page.ts         #   BasePage (visit/expectVisible/click/textContents)
    clients/insurance-app/    # InsuranceApiClient (typed, intent-revealing API surface)
    models/api/               # *.dto.ts transport types
    builders/
      objects/                #   request/object builders (QuoteForge envelope)
      expected/               #   expected-result oracles (rating port of RatingService)
      requests/               #   API-call builders (paged query)
    pages/insurance-app/      # page objects (role/text locators — app has no data-test ids)
    fixtures/insurance-app/   # Playwright fixtures wiring clients + pages
    helpers/                  # unique-id, a11y assertion
  specs/insurance-app/
    api/                      # *.api.spec.ts   → project: insurance-app-api
    ui/                       # *.spec.ts       → projects: insurance-app-ui-{chromium,firefox,webkit}
    a11y/                     # *.a11y.spec.ts  → project: insurance-app-a11y
```

## Path aliases

`@config`, `@core/*`, `@clients/*`, `@pages/*`, `@helpers/*`, `@fixtures/*`,
`@insurance-app-fixtures`, `@models/*`, `@builders/*` (see `tsconfig.json`).

## Prerequisites

- Node.js 24+
- .NET SDK 10 (the Playwright `webServer` builds and runs the app)

## Install

```powershell
cd tests/qa
npm install
npx playwright install
```

## Run

```powershell
npm test                 # everything (API + UI x3 browsers + a11y)
npm run test:api         # API only
npm run test:ui          # UI across chromium, firefox, webkit
npm run test:a11y        # accessibility only
npm run test:smoke       # @smoke-tagged tests
npm run test:critical    # @critical-tagged tests
npm run report           # open the last HTML report
```

Quality gates:

```powershell
npm run typecheck        # tsc --noEmit
npm run lint             # eslint
npm run format:check     # prettier
```

## How it runs the app

Playwright's `webServer` starts the app with:

```
dotnet run --project ../../src/InsuranceIntegration.Api --no-launch-profile --urls http://localhost:5000
```

- `ASPNETCORE_ENVIRONMENT=Development` so the development data is seeded
  (28 quotes, 8 policies, 4 products, 3 source systems).
- `ConnectionStrings__Integration` is overridden to a **dedicated, gitignored SQLite
  database** under `tests/qa/.tmp/qa-e2e.db`, so the suite seeds and mutates its own
  isolated data and **never pollutes the developer database** at
  `src/InsuranceIntegration.Api/integration.db`.
- Logging is quieted to `Warning`.

Because the app is backed by a single shared SQLite file, the suite runs with
`workers: 1` / `fullyParallel: false` for deterministic, isolation-safe runs.

> The suite owns `http://localhost:5000`. Locally, `reuseExistingServer` is on, so if you
> already have a server on that port it will be reused (and its database used). For a clean,
> isolated run, let Playwright start the server (no app running on 5000). In CI the server is
> always started fresh.

Point the suite at an already-running instance with `INSURANCE_APP_BASE_URL`
(see `.env.example`).

## Conventions

- **Locators**: the Blazor UI exposes only semantic HTML (no `data-test` attributes), so
  page objects use role/text locators (`getByRole`, `getByLabel`, `getByText`).
- **Resilience**: API specs never hard-code mutable totals. Seeded counts are asserted as
  floors (`>= 28` quotes, `>= 8` policies); ingest specs use unique envelope ids so reruns
  never collide on the idempotency key.
- **Builders**: independent and fluent — object builders (`QuoteForgeEnvelopeBuilder`),
  expected-result oracles (`expectedRating`, a faithful port of the server's `RatingService`),
  and API-call builders (`PagedQueryBuilder`).
- **Tags**: `@smoke`, `@critical`, `@a11y`.
```
