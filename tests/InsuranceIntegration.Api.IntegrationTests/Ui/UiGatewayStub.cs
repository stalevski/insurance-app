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
/// Hand-written <see cref="IUiGateway"/> test double for the bUnit page tests. The Blazor pages only
/// read through four of the gateway methods, so those return configurable canned data while the
/// remaining members throw — a page that starts depending on them will fail loudly rather than
/// silently rendering empty state. The last <see cref="ListEventsAsync"/> filter arguments are
/// captured so the events page filter wiring can be asserted.
/// </summary>
internal sealed class UiGatewayStub : IUiGateway
{
    public DashboardSummary Dashboard { get; init; } = new();

    public IReadOnlyList<QuoteSnapshotSummary> Quotes { get; init; } = [];

    public IReadOnlyList<PolicySnapshotSummary> Policies { get; init; } = [];

    public IReadOnlyList<DomainEventEntity> Events { get; init; } = [];

    public string? LastEventsAggregateKind { get; private set; }

    public string? LastEventsEventType { get; private set; }

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

    public Task<QuoteSnapshot?> FindQuoteAsync(string quoteReference, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<PolicySnapshot?> FindPolicyAsync(string policyReference, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<DomainEventEntity>> GetAggregateEventsAsync(string aggregateKind, string aggregateKey, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<ProductDefinition>> GetProductsAsync(CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<SourceSystemCatalogItem>> GetSourceSystemsAsync(CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IngestReceipt> DispatchAsync(SourceIngestEnvelope envelope, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<string>> ListTablesAsync(CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<TablePage> QueryTableAsync(string tableName, int skip, int take, CancellationToken ct = default) =>
        throw new NotImplementedException();

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
