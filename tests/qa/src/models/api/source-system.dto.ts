/** A source-system catalog entry as returned by `GET /api/v1/source-systems`. */
export interface SourceSystemDto {
  systemCode: string;
  displayName: string;
  businessPurpose: string;
  messageType: string;
  examplePayload: Record<string, unknown>;
}
