using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Responses.Ingest;
using InsuranceIntegration.Api.Services.Catalog;
using InsuranceIntegration.Api.Services.Policies;
using InsuranceIntegration.Api.Services.Products;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.Snapshots.Policies;
using InsuranceIntegration.Api.Snapshots.Quotes;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Services.Ui;

/// <summary>
/// Read/write facade used by the Blazor UI. Every call runs inside its own short-lived
/// DI scope so the UI never holds a long-lived circuit-scoped <see cref="IntegrationDbContext"/>.
/// </summary>
public interface IUiGateway
{
    Task<DashboardSummary> GetDashboardAsync(CancellationToken ct = default);

    Task<IReadOnlyList<QuoteSnapshotSummary>> ListQuotesAsync(int skip, int take, CancellationToken ct = default);

    Task<QuoteSnapshot?> FindQuoteAsync(string quoteReference, CancellationToken ct = default);

    Task<IReadOnlyList<PolicySnapshotSummary>> ListPoliciesAsync(int skip, int take, CancellationToken ct = default);

    Task<PolicySnapshot?> FindPolicyAsync(string policyReference, CancellationToken ct = default);

    Task<IReadOnlyList<DomainEventEntity>> ListEventsAsync(string? aggregateKind, string? eventType, int skip, int take, CancellationToken ct = default);

    Task<IReadOnlyList<DomainEventEntity>> GetAggregateEventsAsync(string aggregateKind, string aggregateKey, CancellationToken ct = default);

    Task<IReadOnlyList<ProductDefinition>> GetProductsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SourceSystemCatalogItem>> GetSourceSystemsAsync(CancellationToken ct = default);

    Task<IngestReceipt> DispatchAsync(SourceIngestEnvelope envelope, CancellationToken ct = default);

    Task<IReadOnlyList<string>> ListTablesAsync(CancellationToken ct = default);

    Task<TablePage> QueryTableAsync(string tableName, int skip, int take, CancellationToken ct = default);

    Task<PolicyLifecycleResult> CancelPolicyAsync(CancellationRequest request, CancellationToken ct = default);

    Task<PolicyLifecycleResult> EndorsePolicyAsync(EndorsementRequest request, CancellationToken ct = default);

    Task<RenewalResult> RenewPolicyAsync(RenewalRequest request, CancellationToken ct = default);

    Task<PolicyLifecycleResult> ReinstatePolicyAsync(ReinstatementRequest request, CancellationToken ct = default);

    Task<PolicyLifecycleResult> LapsePolicyAsync(LapseRequest request, CancellationToken ct = default);

    Task<PolicyLifecycleResult> NonRenewPolicyAsync(NonRenewalRequest request, CancellationToken ct = default);
}
