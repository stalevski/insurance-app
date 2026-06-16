import { test as base, expect } from '@playwright/test';
import { InsuranceApiClient } from '@clients/insurance-app/insurance-api.client';
import { NavComponent } from '@pages/insurance-app/nav.component';
import { HomePage } from '@pages/insurance-app/home.page';
import { QuotesPage } from '@pages/insurance-app/quotes.page';
import { QuoteDetailPage } from '@pages/insurance-app/quote-detail.page';
import { PoliciesPage } from '@pages/insurance-app/policies.page';
import { IngestPage } from '@pages/insurance-app/ingest.page';
import { EventsPage } from '@pages/insurance-app/events.page';

/**
 * Per-test fixtures for the InsuranceIntegration suite. The API client binds to
 * Playwright's `request` context (which carries the project `baseURL`); page
 * objects bind to the browser `page`. Specs declare only what they use, keeping
 * setup boilerplate out of the test bodies.
 */
type InsuranceFixtures = {
  api: InsuranceApiClient;
  nav: NavComponent;
  homePage: HomePage;
  quotesPage: QuotesPage;
  quoteDetailPage: QuoteDetailPage;
  policiesPage: PoliciesPage;
  ingestPage: IngestPage;
  eventsPage: EventsPage;
};

export const test = base.extend<InsuranceFixtures>({
  api: async ({ request }, use) => {
    await use(new InsuranceApiClient(request));
  },
  nav: async ({ page }, use) => {
    await use(new NavComponent(page));
  },
  homePage: async ({ page }, use) => {
    await use(new HomePage(page));
  },
  quotesPage: async ({ page }, use) => {
    await use(new QuotesPage(page));
  },
  quoteDetailPage: async ({ page }, use) => {
    await use(new QuoteDetailPage(page));
  },
  policiesPage: async ({ page }, use) => {
    await use(new PoliciesPage(page));
  },
  ingestPage: async ({ page }, use) => {
    await use(new IngestPage(page));
  },
  eventsPage: async ({ page }, use) => {
    await use(new EventsPage(page));
  },
});

export { expect };
