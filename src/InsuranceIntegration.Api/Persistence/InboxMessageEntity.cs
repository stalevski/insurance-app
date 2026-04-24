namespace InsuranceIntegration.Api.Persistence;

public sealed class InboxMessageEntity
{
    public string Source { get; set; } = string.Empty;

    public string EnvelopeId { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string HandlerName { get; set; } = string.Empty;

    public string? CorrelationId { get; set; }

    public string ResultJson { get; set; } = string.Empty;

    public DateTime ProcessedAtUtc { get; set; }
}
