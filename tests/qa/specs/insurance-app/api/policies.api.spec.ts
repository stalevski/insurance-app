import { test, expect } from '@insurance-app-fixtures';
import { PagedQueryBuilder } from '@builders/requests/paged-query.builder';
import type { PolicySummaryDto } from '@models/api/policy.dto';

/** A fresh seed contains 8 policies; assert >= so other specs can add more. */
const SEEDED_POLICY_FLOOR = 8;

const assertPolicyShape = (policy: PolicySummaryDto): void => {
  expect(policy.policyReference, 'policyReference').toBeTruthy();
  expect(policy.productCode, 'productCode').toBeTruthy();
  expect(typeof policy.underwritingYear).toBe('number');
  expect(policy.currentPhase, 'currentPhase').toBeTruthy();
  expect(policy.lastUpdatedUtc, 'lastUpdatedUtc').toBeTruthy();
  expect(policy.self, 'self link').toContain(`api/v1/policies/${policy.policyReference}`);
};

test.describe('Policies read endpoints @smoke', () => {
  test('lists seeded policy snapshots with well-formed items', async ({ api }) => {
    const result = await api.policies();

    expect(result.count).toBeGreaterThanOrEqual(SEEDED_POLICY_FLOOR);
    expect(result.items.length).toBeGreaterThan(0);
    result.items.forEach(assertPolicyShape);
  });

  test('retrieves a single policy that matches its list entry', async ({ api }) => {
    const list = await api.policies(new PagedQueryBuilder().take(1));
    const reference = list.items[0]?.policyReference;
    expect(reference, 'expected at least one seeded policy').toBeTruthy();

    const policy = await api.policy(reference);

    expect(policy.policyReference).toBe(reference);
    expect(policy.productCode, 'productCode').toBeTruthy();
    expect(policy.lifecycle.currentPhase, 'lifecycle.currentPhase').toBeTruthy();
    expect(policy.premium, 'premium block').toBeDefined();
  });

  test('each policy links back to its originating quote', async ({ api }) => {
    const result = await api.policies(new PagedQueryBuilder().take(SEEDED_POLICY_FLOOR));

    const missingQuote = result.items.filter((policy) => !policy.quoteReference);
    expect(missingQuote, 'policies missing a quote reference').toEqual([]);
  });

  test('returns 404 for an unknown policy reference', async ({ api }) => {
    const response = await api.policyResponse('POL-DOES-NOT-EXIST-999');

    expect(response.status()).toBe(404);
  });
});
