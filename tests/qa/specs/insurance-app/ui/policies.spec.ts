import { test, expect } from '@insurance-app-fixtures';

test.describe('Policies list @smoke', () => {
  test('lists seeded policies in the snapshot table', async ({ policiesPage }) => {
    await policiesPage.open();

    await expect(policiesPage.heading).toBeVisible();
    await expect(policiesPage.table).toBeVisible();
    expect(await policiesPage.rowCount()).toBeGreaterThan(0);
  });

  test('links each policy back to its originating quote', async ({ policiesPage, page }) => {
    await policiesPage.open();

    // Seeded policies are all bound from a quote, so the first row exposes a quote link.
    const quoteLinkInFirstRow = policiesPage.rows
      .first()
      .getByRole('link')
      .filter({ hasText: /^Q/ })
      .first();

    await expect(quoteLinkInFirstRow).toBeVisible();
    await quoteLinkInFirstRow.click();
    await expect(page).toHaveURL(/\/quotes\//);
  });
});
