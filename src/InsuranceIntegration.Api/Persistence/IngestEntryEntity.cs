namespace InsuranceIntegration.Api.Persistence;

public sealed class IngestEntryEntity
{
    public string Source { get; set; } = string.Empty;

    public string EnvelopeId { get; set; } = string.Empty;

    public string MessageType { get; set; } = string.Empty;

    public string ProcessedBy { get; set; } = string.Empty;

    public string? CorrelationId { get; set; }

    public string OutcomeJson { get; set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; set; }
}
