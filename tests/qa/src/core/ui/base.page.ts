import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Shared base for page objects. Keeps navigation and the most common assertions
 * in one place so concrete pages stay focused on their own locators and flows.
 */
export abstract class BasePage {
  protected constructor(protected readonly page: Page) {}

  protected async visit(path: string): Promise<void> {
    await this.page.goto(path, { waitUntil: 'domcontentloaded' });
  }

  protected async expectVisible(locator: Locator): Promise<void> {
    await expect(locator).toBeVisible();
  }

  protected async click(locator: Locator, options?: { force?: boolean }): Promise<void> {
    await locator.click(options);
  }

  protected async textContents(locator: Locator): Promise<string[]> {
    const values = await locator.allTextContents();
    return values.map((value) => value.trim()).filter(Boolean);
  }
}
