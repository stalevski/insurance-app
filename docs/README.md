# Documentation index

Project documentation, organized by purpose. Start here to find what you need.

> Repository context for AI agents and multi-device continuity lives at the repo root:
> [AGENTS.md](../AGENTS.md) (architecture & conventions) and [PROGRESS.md](../PROGRESS.md)
> (current state / next step). The top-level [README.md](../README.md) is the project overview.

## Guides - how to build, run, test, and deploy

| Document | What it covers |
|----------|----------------|
| [guides/USAGE.md](guides/USAGE.md) | Build, run, and configure the API; per-endpoint request/response shapes; correlation, idempotency, outbox, snapshots, and domain-event semantics. |
| [guides/TESTING.md](guides/TESTING.md) | Running and extending the NUnit suite, test conventions (AAA, `FakeTimeProvider`), the "where is X tested" map, and manual end-to-end recipes. |
| [guides/DEPLOYMENT.md](guides/DEPLOYMENT.md) | Building the Docker image and deploying to a Docker-capable Linux VPS. |

## Reference - payloads and ready-to-run collections

| Document | What it covers |
|----------|----------------|
| [reference/API_EXAMPLES.md](reference/API_EXAMPLES.md) | Manual-testing pack: every example payload paired with its endpoint, mandatory vs optional fields, and the business rules each scenario exercises. |
| [reference/examples/](reference/examples) | Ready-to-POST JSON request payloads referenced throughout the guides. |
| [reference/postman/](reference/postman) | Curated Postman collection + environment, ready to import. |

## Project - planning and known issues

| Document | What it covers |
|----------|----------------|
| [project/KNOWN_ISSUES.md](project/KNOWN_ISSUES.md) | Documented, intentionally-unfixed issues (some are deliberate QA "bait"). |
| [project/FEATURE_PLAN.html](project/FEATURE_PLAN.html) | Candidate-feature backlog with add/defer/skip verdicts - open in a browser. |

## Conventions used in these docs

- Inline source references use **repository-root-relative** paths (e.g. `src/InsuranceIntegration.Api/Program.cs`,
  `tests/dev/InsuranceIntegration.Api.Tests/...`) so they stay accurate regardless of where a doc lives.
- Cross-document links use repository-root-relative paths under `docs/` (e.g. `docs/guides/USAGE.md`).
