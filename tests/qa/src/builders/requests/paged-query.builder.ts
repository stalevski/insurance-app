/**
 * Fluent builder for the paged-list query of the quote and policy read endpoints
 * (`GET /api/v1/quotes?skip&take`, `GET /api/v1/policies?skip&take`).
 *
 * This is the "api call" builder tier: it owns how a request is shaped (here, the
 * query string) independently of the client that sends it and the data it returns.
 * Keeping paging concerns in one place means a change to the endpoint's contract is
 * fixed once, not in every spec.
 */
export class PagedQueryBuilder {
  private skipValue?: number;
  private takeValue?: number;

  skip(skip: number): this {
    this.skipValue = skip;
    return this;
  }

  take(take: number): this {
    this.takeValue = take;
    return this;
  }

  page(pageIndexZeroBased: number, pageSize: number): this {
    this.skipValue = pageIndexZeroBased * pageSize;
    this.takeValue = pageSize;
    return this;
  }

  /** Returns the query parameters as a record (omit unset values). */
  toParams(): Record<string, string> {
    const params: Record<string, string> = {};
    if (this.skipValue !== undefined) {
      params.skip = String(this.skipValue);
    }
    if (this.takeValue !== undefined) {
      params.take = String(this.takeValue);
    }
    return params;
  }

  /** Returns the query string suffix, e.g. `?skip=10&take=5`, or '' when empty. */
  toQueryString(): string {
    const params = new URLSearchParams(this.toParams()).toString();
    return params ? `?${params}` : '';
  }

  /** Appends the query string to a base path, e.g. `buildPath('api/v1/quotes')`. */
  buildPath(basePath: string): string {
    return `${basePath}${this.toQueryString()}`;
  }
}
