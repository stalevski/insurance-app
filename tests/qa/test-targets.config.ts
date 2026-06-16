export type TestTargetConfig = {
  uiBaseUrl: string;
  apiBaseUrl: string;
};

const withTrailingSlash = (value: string): string => (value.endsWith('/') ? value : `${value}/`);

/**
 * The InsuranceIntegration app hosts the Blazor UI and the HTTP API on a single
 * Kestrel process, so the UI and API share one origin. `apiBaseUrl` keeps the
 * trailing slash so relative client paths (`api/v1/quotes`) resolve correctly.
 */
const host = process.env.INSURANCE_APP_BASE_URL ?? 'http://localhost:5000';

export const testTargets = {
  insuranceApp: {
    uiBaseUrl: host,
    apiBaseUrl: withTrailingSlash(host),
  },
} satisfies {
  insuranceApp: TestTargetConfig;
};

export const insuranceAppPort = new URL(testTargets.insuranceApp.uiBaseUrl).port || '5000';
