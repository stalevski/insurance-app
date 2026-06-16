import { test, expect } from '@insurance-app-fixtures';
import { PagedQueryBuilder } from '@builders/requests/paged-query.builder';
import type { QuoteSummaryDto } from '@models/api/quote.dto';

/** A fresh seed contains 28 quotes; other specs may ingest more, so assert >=. */
const SEEDED_QUOTE_FLOOR = 28;

const assertQuoteShape = (quote: QuoteSummaryDto): void => {
  expect(quote.quoteReference, 'quoteReference').toBeTruthy();
  expect(quote.productCode, 'productCode').toBeTruthy();
  expect(typeof quote.underwritingYear).toBe('number');
  expect(quote.currentPhase, 'currentPhase').toBeTruthy();
  expect(typeof quote.isBound).toBe('boolean');
  expect(quote.lastUpdatedUtc, 'lastUpdatedUtc').toBeTruthy();
  expect(quote.self, 'self link').toContain(`api/v1/quotes/${quote.quoteReference}`);
};

test.describe('Quotes read endpoints @smoke', () => {
  test('lists seeded quote snapshots with well-formed items @critical', async ({ api }) => {
    const result = await api.quotes();

    expect(result.count).toBeGreaterThanOrEqual(SEEDED_QUOTE_FLOOR);
    expect(result.items.length).toBeGreaterThan(0);
    result.items.forEach(assertQuoteShape);
  });

  test('retrieves a single quote that matches its list entry', async ({ api }) => {
    const list = await api.quotes(new PagedQueryBuilder().take(1));
    const reference = list.items[0]?.quoteReference;
    expect(reference, 'expected at least one seeded quote').toBeTruthy();

    const quote = await api.quote(reference);

    expect(quote.quoteReference).toBe(reference);
    expect(quote.productCode, 'productCode').toBeTruthy();
    expect(quote.lifecycle.currentPhase, 'lifecycle.currentPhase').toBeTruthy();
    expect(quote.premium, 'premium block').toBeDefined();
  });

  test('bound quotes always carry a policy reference', async ({ api }) => {
    const result = await api.quotes(new PagedQueryBuilder().take(SEEDED_QUOTE_FLOOR));

    const boundWithoutPolicy = result.items.filter((quote) => quote.isBound && !quote.policyReference);
    expect(boundWithoutPolicy, 'bound quotes missing a policy reference').toEqual([]);
  });

  test('honours skip/take paging windows', async ({ api }) => {
    const firstPage = await api.quotes(new PagedQueryBuilder().page(0, 5));
    expect(firstPage.items).toHaveLength(5);

    const secondPage = await api.quotes(new PagedQueryBuilder().page(1, 5));
    expect(secondPage.items).toHaveLength(5);

    // The two windows are disjoint (no quote reference appears in both pages).
    const firstRefs = new Set(firstPage.items.map((quote) => quote.quoteReference));
    const overlap = secondPage.items.filter((quote) => firstRefs.has(quote.quoteReference));
    expect(overlap, 'paging windows should not overlap').toEqual([]);
  });

  test('returns 404 for an unknown quote reference', async ({ api }) => {
    const response = await api.quoteResponse('QF-DOES-NOT-EXIST-999');

    expect(response.status()).toBe(404);
  });
});
