import type { QuoteForgePayloadDto, SourceIngestEnvelopeDto } from '@models/api/ingest.dto';
import { uniqueSuffix } from '@helpers/unique-id';

/**
 * Fluent builder for a QuoteForge {@link SourceIngestEnvelopeDto} (`source=QUOTEFORGE`,
 * `type=QuoteRequest`) — the shape accepted by `POST /api/v1/ingest`.
 *
 * Independent by design: it depends only on the DTOs and a small id helper, never
 * on fixtures, clients, or other builders. Defaults describe a clean, mappable
 * Liability submission with a unique envelope id and quote reference so repeated
 * runs never collide on the per-source idempotency key. `with…` methods tweak only
 * the facets a given test cares about, and {@link build} returns a deep copy so a
 * single builder instance can safely produce many independent envelopes.
 */
export class QuoteForgeEnvelopeBuilder {
  private envelopeId = `qf-${uniqueSuffix()}`;
  private correlationId: string | null = `corr-${uniqueSuffix()}`;
  private occurredAtUtc = '2026-04-24T09:00:00.000Z';
  private payload: QuoteForgePayloadDto;

  constructor() {
    const reference = `QT-${uniqueSuffix().toUpperCase()}`;
    this.payload = {
      quoteReference: reference,
      insuredName: 'Harborline Services',
      productLine: 'Liability',
      brokerCode: 'BRK-44',
      brokerName: 'Summit Risk Partners',
      technicalPremium: 8_200,
      brokerPremium: 8_500,
      currencyCode: 'USD',
      effectiveDate: '2026-05-01',
      expiryDate: '2027-04-30',
      underwritingYear: 2026,
      insuredRevenue: 1_500_000,
      insuredEmployeeCount: 25,
      insuredYearsInBusiness: 8,
    };
  }

  withEnvelopeId(envelopeId: string): this {
    this.envelopeId = envelopeId;
    return this;
  }

  withQuoteReference(quoteReference: string): this {
    this.payload.quoteReference = quoteReference;
    return this;
  }

  withInsuredName(insuredName: string): this {
    this.payload.insuredName = insuredName;
    return this;
  }

  withProductLine(productLine: string): this {
    this.payload.productLine = productLine;
    return this;
  }

  withPremiums(technicalPremium: number, brokerPremium: number): this {
    this.payload.technicalPremium = technicalPremium;
    this.payload.brokerPremium = brokerPremium;
    return this;
  }

  withInsuredRevenue(insuredRevenue: number): this {
    this.payload.insuredRevenue = insuredRevenue;
    return this;
  }

  withUnderwritingYear(underwritingYear: number): this {
    this.payload.underwritingYear = underwritingYear;
    return this;
  }

  withCorrelationId(correlationId: string | null): this {
    this.correlationId = correlationId;
    return this;
  }

  /** The envelope id, which doubles as the idempotency key for this source. */
  get id(): string {
    return this.envelopeId;
  }

  /** The quote reference carried in the payload (the canonical external reference). */
  get quoteReference(): string {
    return this.payload.quoteReference;
  }

  build(): SourceIngestEnvelopeDto<QuoteForgePayloadDto> {
    return {
      id: this.envelopeId,
      source: 'QUOTEFORGE',
      type: 'QuoteRequest',
      schemaVersion: '1.0',
      occurredAtUtc: this.occurredAtUtc,
      correlationId: this.correlationId,
      data: { ...this.payload },
    };
  }
}
