using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Responses.Ingest;
using InsuranceIntegration.Api.Services.Catalog;
using InsuranceIntegration.Api.Services.Events;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Services.Policies;
using InsuranceIntegration.Api.Services.Products;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.Snapshots.Policies;
using InsuranceIntegration.Api.Snapshots.Quotes;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Services.Ui;

public sealed class UiGateway : IUiGateway
{
    private const int MaxPageSize = 200;

    private readonly IServiceScopeFactory _scopeFactory;

    public UiGateway(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<DashboardSummary> GetDashboardAsync(CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        var recent = await db.DomainEvents
            .AsNoTracking()
            .OrderByDescending(record => record.OccurredAtUtc)
            .ThenByDescending(record => record.RecordedAtUtc)
            .Take(10)
            .Select(record => new RecentEvent
            {
                EventType = record.EventType,
                AggregateKind = record.AggregateKind,
                AggregateKey = record.AggregateKey,
                Source = record.Source,
                OccurredAtUtc = record.OccurredAtUtc
            })
            .ToListAsync(ct);

        return new DashboardSummary
        {
            QuoteCount = await db.QuoteSnapshots.AsNoTracking().CountAsync(ct),
            PolicyCount = await db.PolicySnapshots.AsNoTracking().CountAsync(ct),
            BoundQuoteCount = await db.QuoteSnapshots.AsNoTracking().CountAsync(record => record.IsBound, ct),
            DomainEventCount = await db.DomainEvents.AsNoTracking().CountAsync(ct),
            IngestEntryCount = await db.IngestEntries.AsNoTracking().CountAsync(ct),
            PendingOutboxCount = await db.OutboxMessages.AsNoTracking().CountAsync(record => record.DispatchedAtUtc == null, ct),
            RecentEvents = recent
        };
    }

    public async Task<IReadOnlyList<QuoteSnapshotSummary>> ListQuotesAsync(int skip, int take, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IQuoteSnapshotService>();
        return service.List(skip, Clamp(take));
    }

    public async Task<QuoteSnapshot?> FindQuoteAsync(string quoteReference, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IQuoteSnapshotService>();
        return service.Find(quoteReference);
    }

    public async Task<IReadOnlyList<PolicySnapshotSummary>> ListPoliciesAsync(int skip, int take, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IPolicySnapshotService>();
        return service.List(skip, Clamp(take));
    }

    public async Task<PolicySnapshot?> FindPolicyAsync(string policyReference, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IPolicySnapshotService>();
        return service.Find(policyReference);
    }

    public async Task<IReadOnlyList<DomainEventEntity>> ListEventsAsync(string? aggregateKind, string? eventType, int skip, int take, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var log = scope.ServiceProvider.GetRequiredService<IDomainEventLog>();
        return log.List(aggregateKind, eventType, skip, Clamp(take));
    }

    public async Task<IReadOnlyList<DomainEventEntity>> GetAggregateEventsAsync(string aggregateKind, string aggregateKey, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var log = scope.ServiceProvider.GetRequiredService<IDomainEventLog>();
        return log.GetByAggregate(aggregateKind, aggregateKey);
    }

    public async Task<IReadOnlyList<ProductDefinition>> GetProductsAsync(CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<IProductCatalog>();
        return catalog.GetProducts().ToList();
    }

    public async Task<IReadOnlyList<SourceSystemCatalogItem>> GetSourceSystemsAsync(CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<ISourceSystemCatalogService>();
        return catalog.GetSourceSystems().ToList();
    }

    public async Task<IngestReceipt> DispatchAsync(SourceIngestEnvelope envelope, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IIngestDispatcher>();
        return await dispatcher.DispatchAsync(envelope, ct);
    }

    public async Task<IReadOnlyList<string>> ListTablesAsync(CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        return await ReadTableNamesAsync(db, ct);
    }

    public async Task<TablePage> QueryTableAsync(string tableName, int skip, int take, CancellationToken ct = default)
    {
        if (skip < 0) skip = 0;
        take = Clamp(take);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        // Validate the requested table against the live schema so the (necessarily
        // non-parameterizable) identifier interpolated below can never be attacker-controlled.
        var allowedTables = await ReadTableNamesAsync(db, ct);
        var resolved = allowedTables.FirstOrDefault(name => string.Equals(name, tableName, StringComparison.OrdinalIgnoreCase));
        if (resolved is null)
        {
            return new TablePage { TableName = tableName, Skip = skip, Take = take };
        }

        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        try
        {
            var totalRows = await ScalarCountAsync(connection, resolved, ct);

            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM \"{resolved}\" LIMIT $take OFFSET $skip";
            AddParameter(command, "$take", take);
            AddParameter(command, "$skip", skip);

            await using var reader = await command.ExecuteReaderAsync(ct);

            var columns = new List<string>(reader.FieldCount);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }

            var rows = new List<IReadOnlyList<string?>>();
            while (await reader.ReadAsync(ct))
            {
                var row = new string?[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i)?.ToString();
                }

                rows.Add(row);
            }

            return new TablePage
            {
                TableName = resolved,
                Columns = columns,
                Rows = rows,
                TotalRows = totalRows,
                Skip = skip,
                Take = take
            };
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static async Task<IReadOnlyList<string>> ReadTableNamesAsync(IntegrationDbContext db, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' AND name <> '__EFMigrationsHistory' ORDER BY name";

            var names = new List<string>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                names.Add(reader.GetString(0));
            }

            return names;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static async Task<int> ScalarCountAsync(System.Data.Common.DbConnection connection, string table, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM \"{table}\"";
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    public async Task<PolicyLifecycleResult> CancelPolicyAsync(CancellationRequest request, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var lifecycle = scope.ServiceProvider.GetRequiredService<IPolicyLifecycleService>();
        return lifecycle.ApplyCancellation(request);
    }

    public async Task<PolicyLifecycleResult> EndorsePolicyAsync(EndorsementRequest request, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var lifecycle = scope.ServiceProvider.GetRequiredService<IPolicyLifecycleService>();
        return lifecycle.ApplyEndorsement(request);
    }

    public async Task<RenewalResult> RenewPolicyAsync(RenewalRequest request, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var renewal = scope.ServiceProvider.GetRequiredService<IPolicyRenewalService>();
        return renewal.ApplyRenewal(request);
    }

    public async Task<PolicyLifecycleResult> ReinstatePolicyAsync(ReinstatementRequest request, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var lifecycle = scope.ServiceProvider.GetRequiredService<IPolicyLifecycleService>();
        return lifecycle.ApplyReinstatement(request);
    }

    public async Task<PolicyLifecycleResult> LapsePolicyAsync(LapseRequest request, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var lifecycle = scope.ServiceProvider.GetRequiredService<IPolicyLifecycleService>();
        return lifecycle.ApplyLapse(request);
    }

    public async Task<PolicyLifecycleResult> NonRenewPolicyAsync(NonRenewalRequest request, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var lifecycle = scope.ServiceProvider.GetRequiredService<IPolicyLifecycleService>();
        return lifecycle.ApplyNonRenewal(request);
    }

    private static int Clamp(int take) => take switch
    {
        <= 0 => 50,
        > MaxPageSize => MaxPageSize,
        _ => take
    };
}
