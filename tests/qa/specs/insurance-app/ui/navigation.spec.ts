import { test, expect } from '@insurance-app-fixtures';

test.describe('Primary navigation @smoke', () => {
  test.beforeEach(async ({ homePage }) => {
    await homePage.open();
  });

  test('renders the dashboard as the landing page @critical', async ({ homePage }) => {
    await expect(homePage.heading).toBeVisible();
    await expect(homePage.cards).toHaveCount(6);
  });

  test('navigates to Quotes via the sidebar', async ({ nav, quotesPage, page }) => {
    await nav.goToViaNav('Quotes');

    await expect(page).toHaveURL(/\/quotes$/);
    await expect(quotesPage.heading).toBeVisible();
  });

  test('navigates to Policies via the sidebar', async ({ nav, policiesPage, page }) => {
    await nav.goToViaNav('Policies');

    await expect(page).toHaveURL(/\/policies$/);
    await expect(policiesPage.heading).toBeVisible();
  });

  test('navigates to Ingest via the sidebar', async ({ nav, ingestPage, page }) => {
    await nav.goToViaNav('Ingest');

    await expect(page).toHaveURL(/\/ingest$/);
    await expect(ingestPage.heading).toBeVisible();
  });

  test('navigates to Domain events via the sidebar', async ({ nav, eventsPage, page }) => {
    await nav.goToViaNav('Domain events');

    await expect(page).toHaveURL(/\/events$/);
    await expect(eventsPage.heading).toBeVisible();
  });

  test('exposes an external OpenAPI/Swagger link', async ({ nav }) => {
    await expect(nav.swaggerLink()).toHaveAttribute('href', /swagger/);
  });
});
