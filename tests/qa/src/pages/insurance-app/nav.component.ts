import { type Locator, type Page } from '@playwright/test';
import { BasePage } from '@core/ui/base.page';

export type NavTarget = 'Dashboard' | 'Ingest' | 'Quotes' | 'Policies' | 'Domain events';

/**
 * The primary navigation menu rendered in the sidebar on every page. Exposes the
 * nav links by their accessible name and the external OpenAPI/Swagger link.
 */
export class NavComponent extends BasePage {
  readonly nav: Locator;

  constructor(page: Page) {
    super(page);
    this.nav = page.locator('ul.nav');
  }

  link(target: NavTarget): Locator {
    return this.nav.getByRole('link', { name: target, exact: true });
  }

  swaggerLink(): Locator {
    return this.page.getByRole('link', { name: 'OpenAPI / Swagger' });
  }

  async goToViaNav(target: NavTarget): Promise<void> {
    await this.click(this.link(target));
  }
}
