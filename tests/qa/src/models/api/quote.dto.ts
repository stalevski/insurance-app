import type { CoverageDto, PartyDto, PremiumDto, SnapshotHistoryEntryDto } from './common.dto';

/** A quote snapshot summary as returned by `GET /api/v1/quotes`. */
export interface QuoteSummaryDto {
  quoteReference: string;
  policyReference: string | null;
  productCode: string;
  underwritingYear: number;
  currentPhase: string;
  isBound: boolean;
  lastUpdatedUtc: string;
  self: string;
}

/** The quote lifecycle block embedded in the full quote snapshot. */
export interface QuoteLifecycleDto {
  submissionStatus: string;
  quoteStatus: string;
  clearanceDecision: string;
  autoCleared: boolean;
  finalStatus: string;
  currentPhase: string;
  isBound: boolean;
  version: number;
  issuedAtUtc: string | null;
  validUntilUtc: string | null;
  validityDays: number;
  bindRejectionReason: string | null;
}

/** The full quote snapshot returned by `GET /api/v1/quotes/{ref}` (richer than the list summary). */
export interface QuoteSnapshotDto {
  quoteReference: string;
  policyReference: string | null;
  priorPolicyReference: string | null;
  productCode: string;
  underwritingYear: number;
  currencyCode: string;
  insured: PartyDto;
  broker: PartyDto;
  lifecycle: QuoteLifecycleDto;
  premium: PremiumDto;
  coverage: CoverageDto;
  effectiveDate: string | null;
  expiryDate: string | null;
  externalReferences: Record<string, string>;
  history: SnapshotHistoryEntryDto[];
  lastUpdatedUtc: string;
}
