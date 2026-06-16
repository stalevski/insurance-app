import { test, expect } from '@insurance-app-fixtures';

/** The risk-pipeline source systems that must always be catalogued. */
const REQUIRED_SOURCES = ['BINDPOINT', 'CONTOSO_UW', 'QUOTEFORGE'];

test.describe('Source-systems catalog @smoke', () => {
  test('catalogues the core risk-pipeline source systems', async ({ api }) => {
    const systems = await api.sourceSystems();

    const codes = systems.map((system) => system.systemCode);
    for (const required of REQUIRED_SOURCES) {
      expect(codes, `missing source system ${required}`).toContain(required);
    }
  });

  test('each source system carries an example payload and message type', async ({ api }) => {
    const systems = await api.sourceSystems();

    for (const system of systems) {
      expect(system.displayName.trim().length).toBeGreaterThan(0);
      expect(system.businessPurpose.trim().length).toBeGreaterThan(0);
      expect(system.messageType.trim().length).toBeGreaterThan(0);
      expect(system.examplePayload, `${system.systemCode} example payload`).toBeTruthy();
      expect(Object.keys(system.examplePayload).length).toBeGreaterThan(0);
    }
  });
});
