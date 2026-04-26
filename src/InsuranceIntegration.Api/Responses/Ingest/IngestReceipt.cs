namespace InsuranceIntegration.Api.Responses.Ingest;

public sealed class IngestReceipt
{
    public string Source { get; init; } = string.Empty;

    public string EnvelopeId { get; init; } = string.Empty;

    public string MessageType { get; init; } = string.Empty;

    public string ProcessedBy { get; init; } = string.Empty;

    public string? CorrelationId { get; init; }

    public DateTime ReceivedAtUtc { get; init; }

    public string Self { get; init; } = string.Empty;

    public object? Outcome { get; init; }
}
