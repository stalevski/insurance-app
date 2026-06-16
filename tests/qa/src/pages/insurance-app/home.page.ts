import { type Locator, type Page } from '@playwright/test';
import { BasePage } from '@core/ui/base.page';

/** The Dashboard at `/` with six metric cards and a recent-events panel. */
export class HomePage extends BasePage {
  readonly heading: Locator;
  readonly cards: Locator;
  readonly recentEventsPanel: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole('heading', { level: 1, name: 'Dashboard' });
    this.cards = page.locator('.cards .card');
    this.recentEventsPanel = page.locator('.panel', {
      has: page.getByRole('heading', { level: 2, name: 'Recent domain events' }),
    });
  }

  async open(): Promise<void> {
    await this.visit('/');
  }

  /** The numeric metric for a card identified by its label (e.g. "Quotes"). */
  metricFor(label: string): Locator {
    return this.cards.filter({ has: this.page.getByText(label, { exact: true }) }).locator('.metric');
  }

  async metricValue(label: string): Promise<number> {
    const text = (await this.metricFor(label).innerText()).trim();
    return Number(text);
  }
}
