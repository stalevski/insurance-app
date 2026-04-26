namespace InsuranceIntegration.Api.Services.Ingest;

public sealed class IngestContext
{
    public required string Source { get; init; }

    public required string EnvelopeId { get; init; }

    public required string MessageType { get; init; }

    public DateTime ReceivedAtUtc { get; init; }

    public string? CorrelationId { get; init; }
}
