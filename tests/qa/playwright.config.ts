import { defineConfig, devices } from '@playwright/test';
import { existsSync, mkdirSync } from 'node:fs';
import path from 'node:path';
import dotenv from 'dotenv';
import { testTargets } from './test-targets.config';

// `quiet: true` suppresses dotenv's promotional startup tips so test output stays
// deterministic and free of third-party advertising noise in local and CI logs.
dotenv.config({ quiet: true });

const uiBaseUrl = testTargets.insuranceApp.uiBaseUrl;
const apiBaseUrl = testTargets.insuranceApp.apiBaseUrl;

const here = __dirname;

/**
 * The app writes to a SQLite file database. We point the test run at a dedicated,
 * gitignored database under `tests/qa/.tmp/` so the black-box suite seeds and
 * mutates its own isolated data and never pollutes the developer database at
 * `src/InsuranceIntegration.Api/integration.db`. The directory must exist before
 * SQLite opens the file.
 */
const qaDatabaseDir = path.join(here, '.tmp');
if (!existsSync(qaDatabaseDir)) {
  mkdirSync(qaDatabaseDir, { recursive: true });
}
const qaDatabasePath = path.join(qaDatabaseDir, 'qa-e2e.db').replace(/\\/g, '/');

/**
 * Single-worker, non-parallel execution. The app is backed by one shared SQLite
 * file database that cannot tolerate concurrent writes from multiple test files,
 * so all API, UI, and a11y specs run sequentially against one Kestrel process and
 * one database. Correctness and determinism are favoured over raw speed; the
 * black-box surface is small enough that sequential runs stay fast.
 */
export default defineConfig({
  testDir: './specs',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  timeout: 60_000,
  expect: {
    timeout: 10_000,
  },
  reporter: process.env.CI
    ? [
        ['github'],
        ['list'],
        ['html', { open: 'never' }],
        ['junit', { outputFile: 'test-results/junit.xml' }],
      ]
    : [['list'], ['html', { open: 'never' }]],
  use: {
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    testIdAttribute: 'data-test',
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    extraHTTPHeaders: {
      Accept: 'application/json',
    },
  },
  webServer: {
    command:
      'dotnet run --project ../../src/InsuranceIntegration.Api --no-launch-profile --urls http://localhost:5000',
    url: `${uiBaseUrl}/health`,
    cwd: here,
    timeout: 180_000,
    reuseExistingServer: !process.env.CI,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      ConnectionStrings__Integration: `Data Source=${qaDatabasePath}`,
      Logging__LogLevel__Default: 'Warning',
      Logging__LogLevel__Microsoft: 'Warning',
    },
  },
  projects: [
    {
      name: 'insurance-app-api',
      testMatch: /specs\/insurance-app\/api\/.*\.api\.spec\.ts/,
      use: { baseURL: apiBaseUrl },
    },
    {
      name: 'insurance-app-ui-chromium',
      testMatch: /specs\/insurance-app\/ui\/.*\.spec\.ts/,
      use: { ...devices['Desktop Chrome'], baseURL: uiBaseUrl },
    },
    {
      name: 'insurance-app-ui-firefox',
      testMatch: /specs\/insurance-app\/ui\/.*\.spec\.ts/,
      use: { ...devices['Desktop Firefox'], baseURL: uiBaseUrl },
    },
    {
      name: 'insurance-app-ui-webkit',
      testMatch: /specs\/insurance-app\/ui\/.*\.spec\.ts/,
      use: { ...devices['Desktop Safari'], baseURL: uiBaseUrl },
    },
    /**
     * Accessibility checks via `@axe-core/playwright`. Fails on `critical` or
     * `serious` WCAG 2.0/2.1 A+AA violations across the app's primary surfaces;
     * lower-impact findings are surfaced but not enforced.
     */
    {
      name: 'insurance-app-a11y',
      testMatch: /specs\/insurance-app\/a11y\/.*\.a11y\.spec\.ts/,
      use: { ...devices['Desktop Chrome'], baseURL: uiBaseUrl },
    },
  ],
  outputDir: 'test-results',
});
