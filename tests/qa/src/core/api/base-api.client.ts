import { expect, type APIRequestContext, type APIResponse } from '@playwright/test';

/**
 * Thin, reusable wrapper around Playwright's {@link APIRequestContext}. Concrete
 * clients extend this and expose intent-revealing methods; the helpers here keep
 * the happy-path verbs (`get`/`post`/`put`/`patch`/`delete`) terse while still
 * asserting a successful status. Callers that need to inspect non-2xx responses
 * use the raw `request` context directly (exposed to subclasses).
 */
export abstract class BaseApiClient {
  protected constructor(protected readonly request: APIRequestContext) {}

  protected async get<T>(path: string): Promise<T> {
    const response = await this.request.get(path);
    await this.expectOk(response);
    return response.json() as Promise<T>;
  }

  protected async post<TRequest, TResponse>(path: string, payload: TRequest): Promise<TResponse> {
    const response = await this.request.post(path, { data: payload });
    await this.expectOk(response);
    return response.json() as Promise<TResponse>;
  }

  protected async put<TRequest, TResponse>(path: string, payload: TRequest): Promise<TResponse> {
    const response = await this.request.put(path, { data: payload });
    await this.expectOk(response);
    return response.json() as Promise<TResponse>;
  }

  protected async patch<TRequest, TResponse>(path: string, payload: TRequest): Promise<TResponse> {
    const response = await this.request.patch(path, { data: payload });
    await this.expectOk(response);
    return response.json() as Promise<TResponse>;
  }

  protected async delete(path: string): Promise<APIResponse> {
    const response = await this.request.delete(path);
    await this.expectOk(response);
    return response;
  }

  protected async expectOk(response: APIResponse): Promise<void> {
    if (response.ok()) {
      return;
    }

    // On failure, surface the status, status text, and (truncated) response body so
    // the assertion message is self-diagnosing - most non-2xx responses carry a
    // problem-details payload explaining exactly what was rejected.
    const body = await this.readBodySafely(response);
    expect(
      response.ok(),
      `Request to ${response.url()} failed: ${response.status()} ${response.statusText()}\n${body}`,
    ).toBeTruthy();
  }

  private async readBodySafely(response: APIResponse): Promise<string> {
    try {
      const text = await response.text();
      if (text.length === 0) {
        return '<empty response body>';
      }
      return text.length > 2_000 ? `${text.slice(0, 2_000)}... (truncated)` : text;
    } catch {
      return '<unreadable response body>';
    }
  }
}
