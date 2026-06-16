import { test, expect } from '@insurance-app-fixtures';

const SCHEMA_PATHS = [
  'ingest/envelope',
  'ingest/risk-request',
  'canonical/risk-request',
  'final/risk-response',
];

test.describe('JSON schema endpoints', () => {
  for (const schemaPath of SCHEMA_PATHS) {
    test(`serves a JSON schema for ${schemaPath}`, async ({ api }) => {
      const response = await api.schemaResponse(schemaPath);

      expect(response.status()).toBe(200);
      const schema = (await response.json()) as Record<string, unknown>;
      expect(schema, 'schema body').toBeTruthy();
      // A generated JSON schema describes an object with properties.
      const keys = Object.keys(schema);
      expect(keys.length).toBeGreaterThan(0);
      expect('type' in schema || 'properties' in schema || '$schema' in schema).toBeTruthy();
    });
  }

  test('returns 404 for an unknown schema path', async ({ api }) => {
    const response = await api.schemaResponse('does/not-exist');

    expect(response.status()).toBe(404);
  });
});
