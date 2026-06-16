import { test, expect } from '@insurance-app-fixtures';

test.describe('Health endpoint @smoke', () => {
  test('reports the service as healthy @critical', async ({ api }) => {
    const health = await api.health();

    expect(health.status).toBe('Healthy');
    expect(health.service).toBe('InsuranceIntegration.Api');
    expect(health.framework).toContain('.NET');
  });
});
