namespace InsuranceIntegration.Api.FinalMessages.Ingest;

public sealed class IngestAcceptedResult
{
    public string EnvelopeId { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string HandlerName { get; init; } = string.Empty;

    public string? CorrelationId { get; init; }

    public object? Result { get; init; }
}
