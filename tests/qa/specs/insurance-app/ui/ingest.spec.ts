import { test, expect } from '@insurance-app-fixtures';

test.describe('Ingest console @smoke', () => {
  test('pre-fills the editor from a source-system template', async ({ ingestPage }) => {
    await ingestPage.open();
    await expect(ingestPage.heading).toBeVisible();

    await ingestPage.chooseTemplate('QUOTEFORGE');

    // The interactive component fills the editor with a QuoteForge envelope.
    await expect(ingestPage.envelopeEditor).toHaveValue(/QUOTEFORGE/);
  });

  test('submits a templated envelope and shows a receipt @critical', async ({ ingestPage }) => {
    await ingestPage.open();
    await ingestPage.chooseTemplate('QUOTEFORGE');
    await expect(ingestPage.envelopeEditor).toHaveValue(/QUOTEFORGE/);

    await ingestPage.submit();

    await expect(ingestPage.successAlert).toBeVisible({ timeout: 15_000 });
    await expect(ingestPage.successAlert).toContainText('Accepted by');
    await expect(ingestPage.receiptJson).toBeVisible();
    await expect(ingestPage.receiptJson).toContainText('QUOTEFORGE');
  });

  test('reports an error for an invalid envelope', async ({ ingestPage }) => {
    await ingestPage.open();
    await ingestPage.setEnvelopeJson('{ "id": "broken", ');

    await ingestPage.submit();

    await expect(ingestPage.errorAlert).toBeVisible();
  });
});
