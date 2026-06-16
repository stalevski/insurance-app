import { type Locator, type Page } from '@playwright/test';
import { BasePage } from '@core/ui/base.page';

/** A quote detail view at `/quotes/{reference}`. */
export class QuoteDetailPage extends BasePage {
  readonly heading: Locator;
  readonly breadcrumb: Locator;
  readonly notFoundAlert: Locator;
  readonly lifecyclePanel: Locator;
  readonly partiesPanel: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole('heading', { level: 1 });
    this.breadcrumb = page.locator('.breadcrumb');
    this.notFoundAlert = page.locator('.alert.error');
    this.lifecyclePanel = page.locator('.panel', {
      has: page.getByRole('heading', { level: 2, name: 'Lifecycle', exact: true }),
    });
    this.partiesPanel = page.locator('.panel', {
      has: page.getByRole('heading', { level: 2, name: 'Parties & terms' }),
    });
  }

  async open(reference: string): Promise<void> {
    await this.visit(`/quotes/${encodeURIComponent(reference)}`);
  }
}
