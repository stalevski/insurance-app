namespace InsuranceIntegration.Api.Services.Ui;

public sealed class DashboardSummary
{
    public int QuoteCount { get; init; }

    public int PolicyCount { get; init; }

    public int BoundQuoteCount { get; init; }

    public int DomainEventCount { get; init; }

    public int IngestEntryCount { get; init; }

    public int PendingOutboxCount { get; init; }

    public IReadOnlyList<RecentEvent> RecentEvents { get; init; } = [];
}

public sealed class RecentEvent
{
    public string EventType { get; init; } = string.Empty;

    public string AggregateKind { get; init; } = string.Empty;

    public string AggregateKey { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public DateTime OccurredAtUtc { get; init; }
}
