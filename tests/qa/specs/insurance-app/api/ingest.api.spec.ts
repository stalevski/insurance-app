import { test, expect } from '@insurance-app-fixtures';
import { QuoteForgeEnvelopeBuilder } from '@builders/objects/quoteforge-envelope.builder';

test.describe('Ingest gateway @smoke', () => {
  test('accepts a QuoteForge envelope and returns a receipt @critical', async ({ api }) => {
    const envelope = new QuoteForgeEnvelopeBuilder();

    const receipt = await api.ingest(envelope.build());

    expect(receipt.source).toBe('QUOTEFORGE');
    expect(receipt.envelopeId).toBe(envelope.id);
    expect(receipt.messageType).toBe('QuoteRequest');
    expect(receipt.processedBy, 'processedBy').toBeTruthy();
    expect(receipt.receivedAtUtc, 'receivedAtUtc').toBeTruthy();
  });

  test('projects a readable quote snapshot from an ingested envelope @critical', async ({ api }) => {
    const envelope = new QuoteForgeEnvelopeBuilder();
    const reference = envelope.quoteReference;

    await api.ingest(envelope.build());

    // Projection is applied inline during dispatch; poll briefly to stay robust
    // against any future move to asynchronous projection.
    await expect
      .poll(async () => (await api.quoteResponse(reference)).status(), { timeout: 10_000 })
      .toBe(200);

    const quote = await api.quote(reference);
    expect(quote.quoteReference).toBe(reference);
    expect(quote.productCode).toBe('LIABILITY');
  });

  test('is idempotent on the envelope id', async ({ api }) => {
    const envelope = new QuoteForgeEnvelopeBuilder().build();

    const first = await api.ingest(envelope);
    const second = await api.ingest(envelope);

    // A replayed envelope returns the originally stored receipt unchanged.
    expect(second.receivedAtUtc).toBe(first.receivedAtUtc);
    expect(second.envelopeId).toBe(first.envelopeId);
  });

  test('replays a stored receipt via GET', async ({ api }) => {
    const envelope = new QuoteForgeEnvelopeBuilder();
    await api.ingest(envelope.build());

    const replay = await api.ingestReceipt('QUOTEFORGE', envelope.id);

    expect(replay.envelopeId).toBe(envelope.id);
    expect(replay.source).toBe('QUOTEFORGE');
  });

  test('returns 404 when replaying an unknown receipt', async ({ api }) => {
    const response = await api.ingestReceiptResponse('QUOTEFORGE', 'does-not-exist-xyz');

    expect(response.status()).toBe(404);
  });

  test('rejects a syntactically invalid envelope body with 400', async ({ request }) => {
    const response = await request.post('api/v1/ingest', {
      headers: { 'content-type': 'application/json' },
      data: '{ "id": "broken", ',
    });

    expect(response.status()).toBe(400);
  });
});
