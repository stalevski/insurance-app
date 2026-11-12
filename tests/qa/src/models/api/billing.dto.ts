/** Return-premium refund decision as returned by `POST /api/v1/billing/refunds`. */
export interface RefundResultDto {
  policyReference: string;
  transactionType: string;
  transactionReference: string;
  /** Deterministic replacement key (`RFD-CAN-…` / `RFD-END-…`) for replay protection. */
  refundReference: string;
  returnPremium: number;
  /** Part of the return premium the insured never paid, so it cannot be refunded. */
  uncollectedPortion: number;
  refundableBase: number;
  adminFeeWithheld: number;
  offsetAgainstArrears: number;
  netRefundAmount: number;
  /** `Payable` | `NoRefund` | `Exhausted`. */
  outcome: string;
  currencyCode: string;
  payoutMethod: string;
  payableDate: string;
  cappedByCollectedPremium: boolean;
  reasons: string[];
}

/** Request body for `POST /api/v1/billing/refunds`. */
export interface RefundRequestDto {
  policyReference: string;
  transactionType: 'Cancellation' | 'Endorsement';
  transactionReference: string;
  returnPremium: number;
  collectedPremium?: number;
  previouslyRefunded?: number;
  adminFeeWithheld?: number;
  outstandingPremium?: number;
  currencyCode?: string;
  payoutMethod?: 'BankTransfer' | 'Card' | 'Credit';
  effectiveDate?: string;
}
