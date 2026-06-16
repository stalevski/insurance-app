import type { CoverageDto, PartyDto, PremiumDto, SnapshotHistoryEntryDto } from './common.dto';

/** A policy snapshot summary as returned by `GET /api/v1/policies`. */
export interface PolicySummaryDto {
  policyReference: string;
  quoteReference: string | null;
  productCode: string;
  underwritingYear: number;
  currentPhase: string;
  lastUpdatedUtc: string;
  self: string;
}

/** The policy lifecycle block embedded in the full policy snapshot. */
export interface PolicyLifecycleDto {
  submissionStatus: string;
  quoteStatus: string;
  policyStatus: string;
  clearanceDecision: string;
  autoCleared: boolean;
  finalStatus: string;
  currentPhase: string;
}

/** Key policy dates on the full policy snapshot. */
export interface PolicyDatesDto {
  inceptionDate: string | null;
  expiryDate: string | null;
  boundDate: string | null;
}

/** The full policy snapshot returned by `GET /api/v1/policies/{ref}` (richer than the list summary). */
export interface PolicySnapshotDto {
  policyReference: string;
  quoteReference: string | null;
  productCode: string;
  underwritingYear: number;
  currencyCode: string;
  insured: PartyDto;
  broker: PartyDto;
  lifecycle: PolicyLifecycleDto;
  premium: PremiumDto;
  coverage: CoverageDto;
  dates: PolicyDatesDto;
  externalReferences: Record<string, string>;
  history: SnapshotHistoryEntryDto[];
  lastUpdatedUtc: string;
}
