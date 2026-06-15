using InsuranceIntegration.Api.Events;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Responses.Ingest;
using InsuranceIntegration.Api.Services.Catalog;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.Services.Ui;
using InsuranceIntegration.Api.Snapshots.Policies;
using InsuranceIntegration.Api.Snapshots.Quotes;

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

    public static PolicySnapshot PolicyDetail(
        string reference = "POL-PROP-01",
        string? quoteReference = "QF-PROP-01",
        string productCode = "COMMERCIAL_PROPERTY",
        string currentPhase = "Bound") =>
        new()
        {
            PolicyReference = reference,
            QuoteReference = quoteReference,
            ProductCode = productCode,
            UnderwritingYear = 2026,
            CurrencyCode = "USD",
            Insured = new PolicyParty { Code = "INS-01", Name = "Northwind Storage Ltd" },
            Broker = new PolicyParty { Code = "BRK-044", Name = "Summit Risk Partners" },
            Lifecycle = new PolicyLifecycle
            {
                SubmissionStatus = "Received",
                QuoteStatus = "Bound",
                PolicyStatus = "Active",
                ClearanceDecision = "Cleared",
                AutoCleared = true,
                FinalStatus = "Bound",
                CurrentPhase = currentPhase,
            },
            Premium = new PolicyPremium { Base = 11_800m, Adjusted = 11_800m },
            Coverage = new PolicyCoverage
            {
                SectionCount = 3,
                TotalSumInsured = 1_500_000m,
                TotalSectionPremium = 11_800m,
                PremiumAllocationBalanced = true,
            },
            Dates = new PolicyDates
            {
                InceptionDate = new DateOnly(2026, 1, 1),
                ExpiryDate = new DateOnly(2026, 12, 31),
                BoundDate = new DateOnly(2026, 1, 1),
            },
            LastUpdatedUtc = Timestamp,
        };

    public static QuoteSnapshot QuoteDetail(
        string reference = "QF-PROP-01",
        string? policyReference = "POL-PROP-01",
        string productCode = "COMMERCIAL_PROPERTY",
        string currentPhase = "Quoted",
        string? bindRejectionReason = null) =>
        new()
        {
            QuoteReference = reference,
            PolicyReference = policyReference,
            ProductCode = productCode,
            UnderwritingYear = 2026,
            CurrencyCode = "USD",
            Insured = new PolicyParty { Code = "INS-01", Name = "Northwind Storage Ltd" },
            Broker = new PolicyParty { Code = "BRK-044", Name = "Summit Risk Partners" },
            Lifecycle = new QuoteLifecycle
            {
                SubmissionStatus = "Received",
                QuoteStatus = "Quoted",
                ClearanceDecision = "Cleared",
                AutoCleared = true,
                FinalStatus = "Quoted",
                CurrentPhase = currentPhase,
                IsBound = false,
                Version = 1,
                IssuedAtUtc = Timestamp,
                ValidUntilUtc = Timestamp.AddDays(30),
                ValidityDays = 30,
                BindRejectionReason = bindRejectionReason,
            },
            Premium = new PolicyPremium { Base = 11_800m, Adjusted = 11_800m },
            Coverage = new PolicyCoverage
            {
                SectionCount = 3,
                TotalSumInsured = 1_500_000m,
                TotalSectionPremium = 11_800m,
                PremiumAllocationBalanced = true,
            },
            EffectiveDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            LastUpdatedUtc = Timestamp,
        };

    public static TablePage Table(
        string tableName = "Quotes",
        IReadOnlyList<string>? columns = null,
        IReadOnlyList<IReadOnlyList<string?>>? rows = null,
        int? totalRows = null,
        int skip = 0,
        int take = 50)
    {
        columns ??= ["QuoteReference", "ProductCode"];
        rows ??= [["QF-PROP-01", "COMMERCIAL_PROPERTY"], ["QF-LIAB-01", "LIABILITY"]];
        return new TablePage
        {
            TableName = tableName,
            Columns = columns,
            Rows = rows,
            TotalRows = totalRows ?? rows.Count,
            Skip = skip,
            Take = take,
        };
    }

    public static SourceSystemCatalogItem SourceSystem(
        string systemCode = "QUOTEFORGE",
        string displayName = "QuoteForge",
        string messageType = "QuoteRequest",
        object? examplePayload = null) =>
        new()
        {
            SystemCode = systemCode,
            DisplayName = displayName,
            BusinessPurpose = "Inbound quote requests from the QuoteForge underwriting workbench.",
            MessageType = messageType,
            ExamplePayload = examplePayload ?? new { quoteReference = "QT-1" },
        };

    public static IngestReceipt Receipt(
        string source = "QUOTEFORGE",
        string envelopeId = "ui-abc123",
        string messageType = "QuoteRequest",
        string processedBy = "QuoteForgeIngestHandler") =>
        new()
        {
            Source = source,
            EnvelopeId = envelopeId,
            MessageType = messageType,
            ProcessedBy = processedBy,
            ReceivedAtUtc = Timestamp,
            Self = $"/api/v1/ingest/{source}/{envelopeId}",
        };
}
