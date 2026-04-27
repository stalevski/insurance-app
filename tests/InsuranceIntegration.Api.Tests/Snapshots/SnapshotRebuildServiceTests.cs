using InsuranceIntegration.Api.Events;
using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Events;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Services.Matching;
using InsuranceIntegration.Api.Services.Policies;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InsuranceIntegration.Api.Tests.Snapshots;

public sealed class SnapshotRebuildServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IntegrationDbContext> _options;

    public SnapshotRebuildServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var context = new IntegrationDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Test]
    public void RebuildPolicy_ReproducesLifecycleStateFromEvents()
    {
        var (lifecycle, dispatcher) = BuildPipeline();

        // Submit + bind
        dispatcher.Dispatch(BuildSubmission("evt-rb-001", "QT-RB-001"));
        dispatcher.Dispatch(BuildBind("evt-rb-002", "POL-RB-001", "QT-RB-001"));

        // Endorse mid-term, then cancel
        lifecycle.ApplyEndorsement(new EndorsementRequest
        {
            PolicyReference = "POL-RB-001",
            CurrentAnnualPremium = 10000m,
            NewAnnualPremium = 11500m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            EffectiveDate = new DateOnly(2026, 6, 1)
        });
        lifecycle.ApplyCancellation(new CancellationRequest
        {
            PolicyReference = "POL-RB-001",
            AnnualPremium = 11500m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            CancellationDate = new DateOnly(2026, 9, 1),
            Basis = CancellationBasis.ProRata
        });

        using var verifyContext = new IntegrationDbContext(_options);
        var live = new PolicySnapshotService(verifyContext, new PolicySnapshotProjector()).Find("POL-RB-001");
        var rebuild = new SnapshotRebuildService(
            new DomainEventLog(verifyContext),
            new PolicySnapshotProjector(),
            new QuoteSnapshotProjector());

        var result = rebuild.RebuildPolicy("POL-RB-001");

        Assert.Multiple(() =>
        {
            Assert.That(live, Is.Not.Null);
            Assert.That(result.EventsApplied, Is.EqualTo(3), "Bound + Endorsed + Cancelled = 3 policy events");
            Assert.That(result.Snapshot, Is.Not.Null);
            Assert.That(result.Snapshot!.PolicyReference, Is.EqualTo(live!.PolicyReference));
            Assert.That(result.Snapshot.Lifecycle.PolicyStatus, Is.EqualTo(live.Lifecycle.PolicyStatus));
            Assert.That(result.Snapshot.Lifecycle.CurrentPhase, Is.EqualTo(live.Lifecycle.CurrentPhase));
            Assert.That(result.Snapshot.History, Has.Count.EqualTo(live.History.Count),
                "Replayed history must match the live history length");
            Assert.That(result.Snapshot.History.Select(h => h.TransactionType),
                Is.EqualTo(live.History.Select(h => h.TransactionType)));
        });
    }

    [Test]
    public void RebuildQuote_ReproducesQuoteSnapshotFromEvents()
    {
        var (_, dispatcher) = BuildPipeline();
        dispatcher.Dispatch(BuildSubmission("evt-rbq-001", "QT-RBQ-001"));
        dispatcher.Dispatch(BuildBind("evt-rbq-002", "POL-RBQ-001", "QT-RBQ-001"));

        using var verifyContext = new IntegrationDbContext(_options);
        var live = new QuoteSnapshotService(verifyContext, new QuoteSnapshotProjector()).Find("QT-RBQ-001");
        var rebuild = new SnapshotRebuildService(
            new DomainEventLog(verifyContext),
            new PolicySnapshotProjector(),
            new QuoteSnapshotProjector());

        var result = rebuild.RebuildQuote("QT-RBQ-001");

        Assert.Multiple(() =>
        {
            Assert.That(live, Is.Not.Null);
            Assert.That(result.EventsApplied, Is.EqualTo(2), "RiskSubmissionReceived + QuoteBound = 2 quote events");
            Assert.That(result.Snapshot, Is.Not.Null);
            Assert.That(result.Snapshot!.PolicyReference, Is.EqualTo(live!.PolicyReference));
            Assert.That(result.Snapshot.Lifecycle.IsBound, Is.True);
        });
    }

    [Test]
    public void RebuildPolicy_OnUnknownAggregate_ReturnsZeroEvents()
    {
        using var verifyContext = new IntegrationDbContext(_options);
        var rebuild = new SnapshotRebuildService(
            new DomainEventLog(verifyContext),
            new PolicySnapshotProjector(),
            new QuoteSnapshotProjector());

        var result = rebuild.RebuildPolicy("POL-DOES-NOT-EXIST");

        Assert.Multiple(() =>
        {
            Assert.That(result.EventsApplied, Is.EqualTo(0));
            Assert.That(result.Snapshot, Is.Null);
        });
    }

    private static SourceIngestEnvelope BuildSubmission(string envelopeId, string quoteReference)
    {
        return new SourceIngestEnvelope
        {
            Id = envelopeId,
            Source = "CONTOSO_UW",
            Type = "RiskSubmission",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 25, 3, 0, 0, DateTimeKind.Utc),
            Data = JsonSerializer.SerializeToElement(new
            {
                quoteId = quoteReference,
                insuredName = "Rebuild Test Co",
                trade = "CommercialProperty",
                estimatedPremium = 10000m
            })
        };
    }

    private static SourceIngestEnvelope BuildBind(string envelopeId, string policyReference, string quoteReference)
    {
        return new SourceIngestEnvelope
        {
            Id = envelopeId,
            Source = "BINDPOINT",
            Type = "PolicyBindRequest",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 25, 3, 10, 0, DateTimeKind.Utc),
            Data = JsonSerializer.SerializeToElement(new
            {
                policyReference,
                quoteReference,
                insuredName = "Rebuild Test Co",
                productCode = "COMMERCIAL_PROPERTY",
                brokerCode = "BRK-RB",
                brokerName = "Rebuild Brokers",
                brokerHasDelegatedAuthority = true,
                brokerIsPreferredPartner = true,
                boundPremium = 10000m,
                currencyCode = "USD",
                inceptionDate = "2026-01-01",
                expiryDate = "2026-12-31",
                boundDate = "2026-04-25"
            })
        };
    }

    private (IPolicyLifecycleService Lifecycle, IIngestDispatcher Dispatcher) BuildPipeline()
    {
        var context = new IntegrationDbContext(_options);
        var calculator = new LevenshteinDistanceCalculator();
        var registry = new EfCoreSubmissionRegistry(context, TimeProvider.System);
        var clearanceService = new SubmissionClearanceService(registry, calculator);
        var riskFlowService = new RiskFlowService(clearanceService, registry);
        var riskIngestMapper = new RiskIngestMapper(new ISourceRiskMapper[]
        {
            new ContosoRiskMapper(),
            new QuoteForgeRiskMapper(),
            new BindPointRiskMapper()
        });

        var policyProjector = new PolicySnapshotProjector();
        var quoteProjector = new QuoteSnapshotProjector();
        var policyService = new PolicySnapshotService(context, policyProjector);
        var quoteService = new QuoteSnapshotService(context, quoteProjector);
        var eventLog = new DomainEventLog(context);
        var router = new RiskSnapshotRouter(policyService, quoteService, eventLog, TimeProvider.System);
        var adjustment = new PolicyAdjustmentService();
        var lifecycle = new PolicyLifecycleService(adjustment, policyService, riskFlowService, router, context, TimeProvider.System);
        var handler = new RiskIngestHandler(riskIngestMapper, riskFlowService, router, TimeProvider.System);
        var idempotency = new EfCoreIdempotencyStore(context, TimeProvider.System);
        var dispatcher = new IngestDispatcher(new IIngestHandler[] { handler }, idempotency, TimeProvider.System);
        return (lifecycle, dispatcher);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
