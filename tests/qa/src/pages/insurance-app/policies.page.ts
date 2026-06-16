import { type Locator, type Page } from '@playwright/test';
import { BasePage } from '@core/ui/base.page';

/** The Policies list at `/policies` with a snapshot table and a pager. */
export class PoliciesPage extends BasePage {
  readonly heading: Locator;
  readonly table: Locator;
  readonly rows: Locator;
  readonly previousButton: Locator;
  readonly nextButton: Locator;
  readonly emptyState: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole('heading', { level: 1, name: 'Policies' });
    this.table = page.getByRole('table');
    this.rows = this.table.locator('tbody tr');
    this.previousButton = page.getByRole('button', { name: 'Previous' });
    this.nextButton = page.getByRole('button', { name: 'Next' });
    this.emptyState = page.getByText('No policies yet.');
  }

  async open(): Promise<void> {
    await this.visit('/policies');
  }

  policyLink(reference: string): Locator {
    return this.table.getByRole('link', { name: reference, exact: true });
  }

  async openPolicy(reference: string): Promise<void> {
    await this.click(this.policyLink(reference));
  }

  async rowCount(): Promise<number> {
    return this.rows.count();
  }
}
