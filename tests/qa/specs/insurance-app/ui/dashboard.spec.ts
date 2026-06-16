import { test, expect } from '@insurance-app-fixtures';

test.describe('Dashboard @smoke', () => {
  test.beforeEach(async ({ homePage }) => {
    await homePage.open();
  });

  test('shows the six platform metric cards', async ({ homePage }) => {
    await expect(homePage.cards).toHaveCount(6);

    for (const label of [
      'Quotes',
      'Bound quotes',
      'Policies',
      'Domain events',
      'Ingest entries',
      'Pending outbox',
    ]) {
      await expect(homePage.metricFor(label)).toBeVisible();
    }
  });

  test('renders numeric metric values', async ({ homePage }) => {
    const quoteCount = await homePage.metricValue('Quotes');
    const policyCount = await homePage.metricValue('Policies');

    expect(Number.isNaN(quoteCount)).toBe(false);
    expect(Number.isNaN(policyCount)).toBe(false);
    expect(quoteCount).toBeGreaterThanOrEqual(0);
    expect(policyCount).toBeGreaterThanOrEqual(0);
  });

  test('dashboard quote metric agrees with the API', async ({ homePage, api }) => {
    const uiQuoteCount = await homePage.metricValue('Quotes');
    const apiResult = await api.quotes();

    // The dashboard counts every quote; the API list is capped by the page size,
    // so the dashboard total is at least the number returned on the first page.
    expect(uiQuoteCount).toBeGreaterThanOrEqual(apiResult.items.length);
  });

  test('shows the recent domain events panel', async ({ homePage }) => {
    await expect(homePage.recentEventsPanel).toBeVisible();
  });
});
