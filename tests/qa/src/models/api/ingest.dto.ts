/**
 * The QuoteForge-native payload carried inside a `QUOTEFORGE` / `QuoteRequest`
 * envelope. The QuoteForge mapper translates this into the canonical risk model.
 */
export interface QuoteForgePayloadDto {
  quoteReference: string;
  insuredName: string;
  productLine: string;
  brokerCode: string;
  brokerName: string;
  technicalPremium: number;
  brokerPremium: number;
  currencyCode: string;
  effectiveDate: string;
  expiryDate: string;
  underwritingYear: number;
  insuredRevenue: number;
  insuredEmployeeCount: number;
  insuredYearsInBusiness: number;
}

/**
 * The generic source envelope accepted by `POST /api/v1/ingest`. `id` doubles as
 * the per-source idempotency key. `data` carries the source-native payload.
 */
export interface SourceIngestEnvelopeDto<TData = Record<string, unknown>> {
  id: string;
  source: string;
  type: string;
  schemaVersion: string;
  occurredAtUtc: string;
  correlationId?: string | null;
  data: TData;
}

/** The receipt returned by `POST /api/v1/ingest` and `GET /api/v1/ingest/{source}/{id}`. */
export interface IngestReceiptDto {
  envelopeId: string;
  source: string;
  messageType: string;
  processedBy: string;
  receivedAtUtc: string;
  [key: string]: unknown;
}
