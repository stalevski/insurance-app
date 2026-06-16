import { type Locator, type Page } from '@playwright/test';
import { BasePage } from '@core/ui/base.page';

/** The Domain events log at `/events` with aggregate/event-type filters. */
export class EventsPage extends BasePage {
  readonly heading: Locator;
  readonly aggregateFilter: Locator;
  readonly eventTypeFilter: Locator;
  readonly table: Locator;
  readonly rows: Locator;
  readonly emptyState: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole('heading', { level: 1, name: 'Domain events' });
    this.aggregateFilter = page.getByLabel('Aggregate');
    this.eventTypeFilter = page.getByLabel('Event type');
    this.table = page.getByRole('table');
    this.rows = this.table.locator('tbody tr');
    this.emptyState = page.getByText('No events match the current filter.');
  }

  async open(): Promise<void> {
    await this.visit('/events');
  }

  async filterByAggregate(kind: 'Quote' | 'Policy' | 'Claim' | ''): Promise<void> {
    await this.aggregateFilter.selectOption(kind);
  }

  async rowCount(): Promise<number> {
    return this.rows.count();
  }
}
