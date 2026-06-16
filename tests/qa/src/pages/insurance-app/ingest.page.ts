import { expect, type Locator, type Page } from '@playwright/test';
import { BasePage } from '@core/ui/base.page';

/** The Ingest console at `/ingest` for submitting a source envelope to the API. */
export class IngestPage extends BasePage {
  readonly heading: Locator;
  readonly templateSelect: Locator;
  readonly newIdButton: Locator;
  readonly envelopeEditor: Locator;
  readonly submitButton: Locator;
  readonly successAlert: Locator;
  readonly errorAlert: Locator;
  readonly receiptJson: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole('heading', { level: 1, name: 'Ingest a source message' });
    this.templateSelect = page.getByLabel('Template');
    this.newIdButton = page.getByRole('button', { name: 'New envelope id' });
    this.envelopeEditor = page.getByLabel('Envelope JSON');
    this.submitButton = page.getByRole('button', { name: 'Submit envelope' });
    this.successAlert = page.locator('.alert.success');
    this.errorAlert = page.locator('.alert.error');
    this.receiptJson = page.locator('pre.code');
  }

  async open(): Promise<void> {
    await this.visit('/ingest');
  }

  async chooseTemplate(systemCode: string): Promise<void> {
    // This is an interactive Blazor Server component: the @bind:after handler that
    // fills the editor only runs once the SignalR circuit is connected, which can
    // lag behind page load. Re-select (reset to blank, then choose) until the editor
    // is populated, so the step is robust to circuit-connection timing.
    await expect(async () => {
      await this.templateSelect.selectOption('');
      await this.templateSelect.selectOption(systemCode);
      await expect(this.envelopeEditor).toHaveValue(/\S/, { timeout: 2_000 });
    }).toPass({ timeout: 20_000 });
  }

  async setEnvelopeJson(json: string): Promise<void> {
    await this.envelopeEditor.fill(json);
    // Blazor's @bind commits on change; blur the field so the value is captured.
    await this.envelopeEditor.blur();
  }

  async submit(): Promise<void> {
    await this.click(this.submitButton);
  }
}
