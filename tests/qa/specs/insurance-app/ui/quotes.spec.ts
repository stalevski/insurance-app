import { test, expect } from '@insurance-app-fixtures';

test.describe('Quotes list and detail @smoke', () => {
  test('lists seeded quotes in the snapshot table @critical', async ({ quotesPage }) => {
    await quotesPage.open();

    await expect(quotesPage.heading).toBeVisible();
    await expect(quotesPage.table).toBeVisible();
    expect(await quotesPage.rowCount()).toBeGreaterThan(0);
  });

  test('opens a quote detail from a table row', async ({ quotesPage, quoteDetailPage, page }) => {
    await quotesPage.open();

    const firstLink = quotesPage.rows.first().getByRole('link').first();
    const reference = (await firstLink.innerText()).trim();
    await firstLink.click();

    await expect(page).toHaveURL(new RegExp(`/quotes/${reference}$`));
    await expect(quoteDetailPage.heading).toContainText(reference);
    await expect(quoteDetailPage.lifecyclePanel).toBeVisible();
  });

  test('shows a not-found message for an unknown quote', async ({ quoteDetailPage }) => {
    await quoteDetailPage.open('QF-DOES-NOT-EXIST-123');

    await expect(quoteDetailPage.notFoundAlert).toBeVisible();
    await expect(quoteDetailPage.notFoundAlert).toContainText('was not found');
  });
});
