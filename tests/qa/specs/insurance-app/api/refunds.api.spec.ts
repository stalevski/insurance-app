import { test, expect } from '@insurance-app-fixtures';
import { uniqueSuffix } from '@helpers/unique-id';
import type { RefundRequestDto } from '@models/api/billing.dto';

/**
 * `POST /api/v1/billing/refunds` turns the return premium produced by a cancellation or
 * endorsement into a payable refund decision. It is a pure calculation - no money moves - so these
 * tests pin the settlement rules that matter to a finance team:
 *
 * 1. you cannot refund premium that was never collected,
 * 2. the admin fee is withheld (but never more than the refund itself),
 * 3. arrears are recovered before anything is paid out,
 * 4. the payable date follows the payout channel's clearing time,
 * 5. replaying the same adjustment yields the same refund reference (no double payment).
 */
test.describe('Billing refunds', () => {
  const buildRequest = (overrides: Partial<RefundRequestDto> = {}): RefundRequestDto => ({
    policyReference: `POL-QA-${uniqueSuffix()}`,
    transactionType: 'Cancellation',
    transactionReference: `CAN-${uniqueSuffix()}`,
    returnPremium: 300,
    collectedPremium: 1_200,
    ...overrides,
  });

  test('pays the full return premium when nothing is withheld @smoke', async ({ api }) => {
    const request = buildRequest({ effectiveDate: '2026-03-01' });

    const result = await api.refund(request);

    expect(result.outcome).toBe('Payable');
    expect(result.netRefundAmount).toBe(300);
    expect(result.refundReference).toBe(`RFD-CAN-${request.transactionReference}`);
    // Bank transfer clears in five days.
    expect(result.payableDate).toBe('2026-03-06');
    expect(result.currencyCode).toBe('USD');
  });

  test('caps the refund at premium actually collected', async ({ api }) => {
    const result = await api.refund(buildRequest({ returnPremium: 900, collectedPremium: 600 }));

    expect(result.refundableBase).toBe(600);
    expect(result.uncollectedPortion).toBe(300);
    expect(result.cappedByCollectedPremium).toBe(true);
    expect(result.netRefundAmount).toBe(600);
  });

  test('withholds the admin fee and recovers arrears before paying out', async ({ api }) => {
    const result = await api.refund(
      buildRequest({
        returnPremium: 500,
        collectedPremium: 500,
        adminFeeWithheld: 50,
        outstandingPremium: 100,
        payoutMethod: 'Card',
        effectiveDate: '2026-04-01',
      }),
    );

    expect(result.adminFeeWithheld).toBe(50);
    expect(result.offsetAgainstArrears).toBe(100);
    expect(result.netRefundAmount).toBe(350);
    expect(result.payoutMethod).toBe('Card');
    expect(result.payableDate).toBe('2026-04-04');
    expect(result.reasons.join(' | ')).toContain('Offset against outstanding premium: 100');
  });

  test('reports Exhausted when every collected unit was already refunded', async ({ api }) => {
    const result = await api.refund(
      buildRequest({ returnPremium: 400, collectedPremium: 900, previouslyRefunded: 900 }),
    );

    expect(result.outcome).toBe('Exhausted');
    expect(result.refundableBase).toBe(0);
    expect(result.netRefundAmount).toBe(0);
  });

  test('is idempotent for a replayed adjustment - same reference, same amount', async ({ api }) => {
    const request = buildRequest();
    const [first, second] = [await api.refund(request), await api.refund(request)];

    expect(second.refundReference).toBe(first.refundReference);
    expect(second.netRefundAmount).toBe(first.netRefundAmount);
    expect(second.outcome).toBe(first.outcome);
  });

  test('rejects an unsupported transaction type with 400 and a problem detail', async ({ api }) => {
    const response = await api.refundResponse(
      buildRequest({ transactionType: 'Refund' as RefundRequestDto['transactionType'] }),
    );

    expect(response.status()).toBe(400);
    const problem = (await response.json()) as { detail?: string };
    expect(problem.detail).toContain("Expected 'Cancellation' or 'Endorsement'");
  });

  test('rejects negative money with 400', async ({ api }) => {
    const response = await api.refundResponse(buildRequest({ adminFeeWithheld: -10 }));

    expect(response.status()).toBe(400);
  });
});
