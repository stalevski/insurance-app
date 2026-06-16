using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Responses.Ingest;
using InsuranceIntegration.Api.Services.Catalog;
using InsuranceIntegration.Api.Services.Policies;
using InsuranceIntegration.Api.Services.Products;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.Services.Ui;
using InsuranceIntegration.Api.Snapshots.Policies;
using InsuranceIntegration.Api.Snapshots.Quotes;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.IntegrationTests.Ui;

/// <summary>
/// Hand-written <see cref="IUiGateway"/> test double for the bUnit page tests. Every method a page
/// renders through returns configurable canned data (set via the init-only properties) and captures
/// the arguments worth asserting on. The members no page reads — the lifecycle transaction calls and
/// <see cref="GetProductsAsync"/> — still throw, so a page that starts depending on them fails loudly
/// rather than silently rendering empty state.
/// </summary>
internal sealed class UiGatewayStub : IUiGateway
{
    public DashboardSummary Dashboard { get; init; } = new();

    public IReadOnlyList<QuoteSnapshotSummary> Quotes { get; init; } = [];

    public IReadOnlyList<PolicySnapshotSummary> Policies { get; init; } = [];

    public IReadOnlyList<DomainEventEntity> Events { get; init; } = [];

    public QuoteSnapshot? QuoteDetail { get; init; }

    public PolicySnapshot? PolicyDetail { get; init; }

    public IReadOnlyList<DomainEventEntity> AggregateEvents { get; init; } = [];

    public IReadOnlyList<SourceSystemCatalogItem> SourceSystems { get; init; } = [];

    public IReadOnlyList<string> Tables { get; init; } = [];

    public TablePage TablePageResult { get; init; } = new();

    public IngestReceipt? DispatchReceipt { get; init; }

    public string? LastEventsAggregateKind { get; private set; }

    public string? LastEventsEventType { get; private set; }

    public string? LastFindQuoteReference { get; private set; }

    public string? LastFindPolicyReference { get; private set; }

    public string? LastAggregateEventsKind { get; private set; }

    public string? LastAggregateEventsKey { get; private set; }

    public SourceIngestEnvelope? LastDispatchedEnvelope { get; private set; }

    public string? LastQueriedTable { get; private set; }

    public int LastQueriedSkip { get; private set; }

    public Task<DashboardSummary> GetDashboardAsync(CancellationToken ct = default) =>
        Task.FromResult(Dashboard);

    public Task<IReadOnlyList<QuoteSnapshotSummary>> ListQuotesAsync(int skip, int take, CancellationToken ct = default) =>
        Task.FromResult(Quotes);

    public Task<IReadOnlyList<PolicySnapshotSummary>> ListPoliciesAsync(int skip, int take, CancellationToken ct = default) =>
        Task.FromResult(Policies);

    public Task<IReadOnlyList<DomainEventEntity>> ListEventsAsync(string? aggregateKind, string? eventType, int skip, int take, CancellationToken ct = default)
    {
        LastEventsAggregateKind = aggregateKind;
        LastEventsEventType = eventType;
        return Task.FromResult(Events);
    }

    public Task<QuoteSnapshot?> FindQuoteAsync(string quoteReference, CancellationToken ct = default)
    {
        LastFindQuoteReference = quoteReference;
        return Task.FromResult(QuoteDetail);
    }

    public Task<PolicySnapshot?> FindPolicyAsync(string policyReference, CancellationToken ct = default)
    {
        LastFindPolicyReference = policyReference;
        return Task.FromResult(PolicyDetail);
    }

    public Task<IReadOnlyList<DomainEventEntity>> GetAggregateEventsAsync(string aggregateKind, string aggregateKey, CancellationToken ct = default)
    {
        LastAggregateEventsKind = aggregateKind;
        LastAggregateEventsKey = aggregateKey;
        return Task.FromResult(AggregateEvents);
    }

    public Task<IReadOnlyList<ProductDefinition>> GetProductsAsync(CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<SourceSystemCatalogItem>> GetSourceSystemsAsync(CancellationToken ct = default) =>
        Task.FromResult(SourceSystems);

    public Task<IngestReceipt> DispatchAsync(SourceIngestEnvelope envelope, CancellationToken ct = default)
    {
        LastDispatchedEnvelope = envelope;
        var receipt = DispatchReceipt ?? new IngestReceipt
        {
            Source = envelope.Source,
            EnvelopeId = envelope.Id,
            MessageType = envelope.Type,
            ProcessedBy = "StubHandler",
            CorrelationId = envelope.CorrelationId,
            ReceivedAtUtc = envelope.OccurredAtUtc,
            Self = $"/api/v1/ingest/{envelope.Source}/{envelope.Id}",
        };
        return Task.FromResult(receipt);
    }

    public Task<IReadOnlyList<string>> ListTablesAsync(CancellationToken ct = default) =>
        Task.FromResult(Tables);

    public Task<TablePage> QueryTableAsync(string tableName, int skip, int take, CancellationToken ct = default)
    {
        LastQueriedTable = tableName;
        LastQueriedSkip = skip;
        return Task.FromResult(TablePageResult);
    }

    public Task<PolicyLifecycleResult> CancelPolicyAsync(CancellationRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<PolicyLifecycleResult> EndorsePolicyAsync(EndorsementRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<RenewalResult> RenewPolicyAsync(RenewalRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<PolicyLifecycleResult> ReinstatePolicyAsync(ReinstatementRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<PolicyLifecycleResult> LapsePolicyAsync(LapseRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<PolicyLifecycleResult> NonRenewPolicyAsync(NonRenewalRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();
}
