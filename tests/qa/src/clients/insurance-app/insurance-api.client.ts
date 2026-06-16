import type { APIRequestContext, APIResponse } from '@playwright/test';
import { BaseApiClient } from '@core/api/base-api.client';
import type { HealthDto, PagedResultDto } from '@models/api/common.dto';
import type { ProductDto } from '@models/api/product.dto';
import type { SourceSystemDto } from '@models/api/source-system.dto';
import type { QuoteSnapshotDto, QuoteSummaryDto } from '@models/api/quote.dto';
import type { PolicySnapshotDto, PolicySummaryDto } from '@models/api/policy.dto';
import type { IngestReceiptDto, SourceIngestEnvelopeDto } from '@models/api/ingest.dto';
import { PagedQueryBuilder } from '@builders/requests/paged-query.builder';

/**
 * Typed, intent-revealing client for the InsuranceIntegration HTTP API. Happy-path
 * methods inherit status assertions from {@link BaseApiClient}; the `*Response`
 * variants return the raw {@link APIResponse} so negative tests can assert on
 * non-2xx status codes and problem details without the base class throwing first.
 */
export class InsuranceApiClient extends BaseApiClient {
  constructor(request: APIRequestContext) {
    super(request);
  }

  // ---- Diagnostics -------------------------------------------------------

  health(): Promise<HealthDto> {
    return this.get<HealthDto>('health');
  }

  // ---- Catalogs ----------------------------------------------------------

  products(): Promise<ProductDto[]> {
    return this.get<ProductDto[]>('api/v1/products');
  }

  sourceSystems(): Promise<SourceSystemDto[]> {
    return this.get<SourceSystemDto[]>('api/v1/source-systems');
  }

  // ---- Quotes ------------------------------------------------------------

  quotes(query?: PagedQueryBuilder): Promise<PagedResultDto<QuoteSummaryDto>> {
    const path = (query ?? new PagedQueryBuilder()).buildPath('api/v1/quotes');
    return this.get<PagedResultDto<QuoteSummaryDto>>(path);
  }

  quote(reference: string): Promise<QuoteSnapshotDto> {
    return this.get<QuoteSnapshotDto>(`api/v1/quotes/${encodeURIComponent(reference)}`);
  }

  quoteResponse(reference: string): Promise<APIResponse> {
    return this.request.get(`api/v1/quotes/${encodeURIComponent(reference)}`);
  }

  // ---- Policies ----------------------------------------------------------

  policies(query?: PagedQueryBuilder): Promise<PagedResultDto<PolicySummaryDto>> {
    const path = (query ?? new PagedQueryBuilder()).buildPath('api/v1/policies');
    return this.get<PagedResultDto<PolicySummaryDto>>(path);
  }

  policy(reference: string): Promise<PolicySnapshotDto> {
    return this.get<PolicySnapshotDto>(`api/v1/policies/${encodeURIComponent(reference)}`);
  }

  policyResponse(reference: string): Promise<APIResponse> {
    return this.request.get(`api/v1/policies/${encodeURIComponent(reference)}`);
  }

  // ---- Ingest ------------------------------------------------------------

  ingest<TData>(envelope: SourceIngestEnvelopeDto<TData>): Promise<IngestReceiptDto> {
    return this.post<SourceIngestEnvelopeDto<TData>, IngestReceiptDto>('api/v1/ingest', envelope);
  }

  ingestResponse<TData>(envelope: SourceIngestEnvelopeDto<TData>): Promise<APIResponse> {
    return this.request.post('api/v1/ingest', { data: envelope });
  }

  ingestReceipt(source: string, envelopeId: string): Promise<IngestReceiptDto> {
    return this.get<IngestReceiptDto>(
      `api/v1/ingest/${encodeURIComponent(source)}/${encodeURIComponent(envelopeId)}`,
    );
  }

  ingestReceiptResponse(source: string, envelopeId: string): Promise<APIResponse> {
    return this.request.get(
      `api/v1/ingest/${encodeURIComponent(source)}/${encodeURIComponent(envelopeId)}`,
    );
  }

  // ---- Schemas -----------------------------------------------------------

  schemaResponse(schemaPath: string): Promise<APIResponse> {
    return this.request.get(`api/v1/schemas/${schemaPath}`);
  }
}
