/** A paged read-model list as returned by the quote and policy read endpoints. */
export interface PagedResultDto<T> {
  items: T[];
  count: number;
}

/** Response shape of `GET /health`. */
export interface HealthDto {
  status: string;
  service: string;
  framework: string;
}

/** A named party (insured or broker) on a quote/policy snapshot. */
export interface PartyDto {
  code: string | null;
  name: string | null;
  tradingName: string | null;
}

/** Premium figures on a snapshot. `decimal?` on the server maps to `number | null`. */
export interface PremiumDto {
  base: number | null;
  adjusted: number | null;
}

/** Coverage rollup on a snapshot. */
export interface CoverageDto {
  sectionCount: number;
  totalSumInsured: number;
  totalSectionPremium: number;
  premiumAllocationBalanced: boolean;
  warnings?: string[];
}

/** A single provenance entry in a snapshot's history. */
export interface SnapshotHistoryEntryDto {
  atUtc: string;
  source: string;
  messageType: string;
  envelopeId: string;
  transactionType: string;
}
