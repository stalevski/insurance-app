using InsuranceIntegration.Api.Events;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.Services.Ui;

namespace InsuranceIntegration.Api.IntegrationTests.Ui;

/// <summary>
/// Factory helpers for the read-model shapes the Blazor pages render. Centralising construction
/// keeps the bUnit tests focused on the markup under assertion and means a new required member on
/// any of these types only needs a default supplied in one place.
/// </summary>
internal static class UiTestData
{
    private static readonly DateTime Timestamp = new(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc);

    public static QuoteSnapshotSummary Quote(
        string reference = "QF-PROP-01",
        string productCode = "COMMERCIAL_PROPERTY",
        int underwritingYear = 2026,
        string currentPhase = "Quoted",
        bool isBound = false) =>
        new()
        {
            QuoteReference = reference,
            ProductCode = productCode,
            UnderwritingYear = underwritingYear,
            CurrentPhase = currentPhase,
            IsBound = isBound,
            LastUpdatedUtc = Timestamp,
            Self = $"/api/v1/quotes/{reference}",
        };

    public static PolicySnapshotSummary Policy(
        string reference = "POL-PROP-01",
        string? quoteReference = "QF-PROP-01",
        string productCode = "COMMERCIAL_PROPERTY",
        int underwritingYear = 2026,
        string currentPhase = "Bound") =>
        new()
        {
            PolicyReference = reference,
            QuoteReference = quoteReference,
            ProductCode = productCode,
            UnderwritingYear = underwritingYear,
            CurrentPhase = currentPhase,
            LastUpdatedUtc = Timestamp,
            Self = $"/api/v1/policies/{reference}",
        };

    public static DomainEventEntity Event(
        string eventType = DomainEventType.QuoteIssued,
        string aggregateKind = DomainEventAggregateKind.Quote,
        string aggregateKey = "QF-PROP-01",
        string source = "QUOTEFORGE",
        string? envelopeId = "qf-1001",
        string? correlationId = "corr-1001") =>
        new()
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            AggregateKind = aggregateKind,
            AggregateKey = aggregateKey,
            Source = source,
            EnvelopeId = envelopeId,
            CorrelationId = correlationId,
            OccurredAtUtc = Timestamp,
            RecordedAtUtc = Timestamp,
            PayloadJson = "{}",
        };

    public static RecentEvent Recent(
        string eventType = DomainEventType.QuoteIssued,
        string aggregateKind = DomainEventAggregateKind.Quote,
        string aggregateKey = "QF-PROP-01",
        string source = "QUOTEFORGE") =>
        new()
        {
            EventType = eventType,
            AggregateKind = aggregateKind,
            AggregateKey = aggregateKey,
            Source = source,
            OccurredAtUtc = Timestamp,
        };
}
