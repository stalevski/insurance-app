import { type Locator, type Page } from '@playwright/test';
import { BasePage } from '@core/ui/base.page';

/** The Quotes list at `/quotes` with a snapshot table and a pager. */
export class QuotesPage extends BasePage {
  readonly heading: Locator;
  readonly table: Locator;
  readonly rows: Locator;
  readonly previousButton: Locator;
  readonly nextButton: Locator;
  readonly showingLabel: Locator;
  readonly emptyState: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole('heading', { level: 1, name: 'Quotes' });
    this.table = page.getByRole('table');
    this.rows = this.table.locator('tbody tr');
    this.previousButton = page.getByRole('button', { name: 'Previous' });
    this.nextButton = page.getByRole('button', { name: 'Next' });
    this.showingLabel = page.locator('.pager .muted');
    this.emptyState = page.getByText('No quotes yet.');
  }

  async open(): Promise<void> {
    await this.visit('/quotes');
  }

  /** The row link for a quote reference (e.g. "QF-CYB-01"). */
  quoteLink(reference: string): Locator {
    return this.table.getByRole('link', { name: reference, exact: true });
  }

  async openQuote(reference: string): Promise<void> {
    await this.click(this.quoteLink(reference));
  }

  async rowCount(): Promise<number> {
    return this.rows.count();
  }
}
