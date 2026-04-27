using InsuranceIntegration.Api.Events;
using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Events;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Services.Matching;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InsuranceIntegration.Api.Tests.Snapshots;

public sealed class SnapshotPipelineTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IntegrationDbContext> _options;

    public SnapshotPipelineTests()
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
    public void IngestingThreeRelatedEnvelopes_ProducesSinglePolicySnapshotWithMergedFields()
    {
        var (handler, dispatcher) = BuildPipeline();

        // 1) Contoso RiskSubmission - establishes the quote
        dispatcher.Dispatch(new SourceIngestEnvelope
        {
            Id = "evt-contoso-001",
            Source = "CONTOSO_UW",
            Type = "RiskSubmission",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 25, 3, 0, 0, DateTimeKind.Utc),
            CorrelationId = "corr-1",
            Data = JsonSerializer.SerializeToElement(new
            {
                quoteId = "QT-2201",
                insuredName = "Northwind Storage Ltd",
                trade = "CommercialProperty",
                estimatedPremium = 12500m
            })
        });

        // 2) QuoteForge QuoteRequest - confirms the quote with broker info
        dispatcher.Dispatch(new SourceIngestEnvelope
        {
            Id = "evt-qf-001",
            Source = "QUOTEFORGE",
            Type = "QuoteRequest",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 25, 3, 5, 0, DateTimeKind.Utc),
            CorrelationId = "corr-2",
            Data = JsonSerializer.SerializeToElement(new
            {
                quoteReference = "QT-2201",
                insuredName = "Northwind Storage Ltd",
                productLine = "COMMERCIAL_PROPERTY",
                brokerCode = "BRK-044",
                brokerName = "Harbor Broking",
                technicalPremium = 12000m,
                brokerPremium = 12500m,
                currencyCode = "USD",
                effectiveDate = "2026-05-01",
                expiryDate = "2027-04-30",
                underwritingYear = 2026,
                insuredRevenue = 2_200_000m,
                insuredEmployeeCount = 40,
                insuredYearsInBusiness = 8
            })
        });

        // 3) BindPoint PolicyBindRequest - converts the quote into a policy
        dispatcher.Dispatch(new SourceIngestEnvelope
        {
            Id = "evt-bp-001",
            Source = "BINDPOINT",
            Type = "PolicyBindRequest",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 25, 3, 10, 0, DateTimeKind.Utc),
            CorrelationId = "corr-3",
            Data = JsonSerializer.SerializeToElement(new
            {
                policyReference = "POL-7781",
                quoteReference = "QT-2201",
                insuredName = "Northwind Storage Ltd",
                productCode = "COMMERCIAL_PROPERTY",
                brokerCode = "BRK-044",
                brokerName = "Harbor Broking",
                brokerHasDelegatedAuthority = true,
                brokerIsPreferredPartner = true,
                boundPremium = 12500m,
                currencyCode = "USD",
                inceptionDate = "2026-05-01",
                expiryDate = "2027-04-30",
                boundDate = "2026-04-25",
                installmentCount = 4
            })
        });

        // PolicySnapshot must exist for POL-7781 and merge knowledge from all three sources.
        using var verifyContext = new IntegrationDbContext(_options);
        var policyService = new PolicySnapshotService(verifyContext, new PolicySnapshotProjector());
        var snapshot = policyService.Find("POL-7781");

        Assert.That(snapshot, Is.Not.Null, "PolicySnapshot for POL-7781 must exist after bind");
        Assert.That(snapshot!.PolicyReference, Is.EqualTo("POL-7781"));
        Assert.That(snapshot.QuoteReference, Is.EqualTo("QT-2201"));
        Assert.That(snapshot.Insured.Name, Is.EqualTo("Northwind Storage Ltd"));
        Assert.That(snapshot.History, Has.Count.EqualTo(1), "Only the bind event references the policy key");
        Assert.That(snapshot.History[0].Source, Is.EqualTo("BINDPOINT"));

        // QuoteSnapshot for QT-2201 should reflect all three sources (because quoteRef appears in all three).
        var quoteService = new QuoteSnapshotService(verifyContext, new QuoteSnapshotProjector());
        var quote = quoteService.Find("QT-2201");

        Assert.That(quote, Is.Not.Null);
        Assert.That(quote!.QuoteReference, Is.EqualTo("QT-2201"));
        Assert.That(quote.PolicyReference, Is.EqualTo("POL-7781"), "Bind should backfill the policyReference");
        Assert.That(quote.Lifecycle.IsBound, Is.True);
        Assert.That(quote.Lifecycle.CurrentPhase, Is.EqualTo("Bound"), "Quote phase must advance to Bound once a policy reference is tied to it");
        Assert.That(quote.History, Has.Count.EqualTo(3));
        Assert.That(quote.History.Select(h => h.Source), Is.EquivalentTo(new[] { "CONTOSO_UW", "QUOTEFORGE", "BINDPOINT" }));
        Assert.That(quote.ExternalReferences.Keys, Is.EquivalentTo(new[] { "CONTOSO_UW", "QUOTEFORGE", "BINDPOINT" }));
    }

    [Test]
    public void IngestingThreeRelatedEnvelopes_WritesDomainEventsForEachAffectedAggregate()
    {
        var (_, dispatcher) = BuildPipeline();

        dispatcher.Dispatch(new SourceIngestEnvelope
        {
            Id = "evt-contoso-evt-001",
            Source = "CONTOSO_UW",
            Type = "RiskSubmission",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 25, 3, 0, 0, DateTimeKind.Utc),
            CorrelationId = "corr-evt-1",
            Data = JsonSerializer.SerializeToElement(new
            {
                quoteId = "QT-EVT-001",
                insuredName = "Eventful Storage Ltd",
                trade = "CommercialProperty",
                estimatedPremium = 8000m
            })
        });

        dispatcher.Dispatch(new SourceIngestEnvelope
        {
            Id = "evt-qf-evt-001",
            Source = "QUOTEFORGE",
            Type = "QuoteRequest",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 25, 3, 5, 0, DateTimeKind.Utc),
            CorrelationId = "corr-evt-2",
            Data = JsonSerializer.SerializeToElement(new
            {
                quoteReference = "QT-EVT-001",
                insuredName = "Eventful Storage Ltd",
                productLine = "COMMERCIAL_PROPERTY",
                brokerCode = "BRK-EVT",
                brokerName = "Eventful Brokers",
                technicalPremium = 8000m,
                brokerPremium = 8500m,
                currencyCode = "USD",
                effectiveDate = "2026-05-01",
                expiryDate = "2027-04-30",
                underwritingYear = 2026,
                insuredRevenue = 1_000_000m,
                insuredEmployeeCount = 10,
                insuredYearsInBusiness = 6
            })
        });

        dispatcher.Dispatch(new SourceIngestEnvelope
        {
            Id = "evt-bp-evt-001",
            Source = "BINDPOINT",
            Type = "PolicyBindRequest",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 25, 3, 10, 0, DateTimeKind.Utc),
            CorrelationId = "corr-evt-3",
            Data = JsonSerializer.SerializeToElement(new
            {
                policyReference = "POL-EVT-001",
                quoteReference = "QT-EVT-001",
                insuredName = "Eventful Storage Ltd",
                productCode = "COMMERCIAL_PROPERTY",
                brokerCode = "BRK-EVT",
                brokerName = "Eventful Brokers",
                brokerHasDelegatedAuthority = true,
                brokerIsPreferredPartner = true,
                boundPremium = 8500m,
                currencyCode = "USD",
                inceptionDate = "2026-05-01",
                expiryDate = "2027-04-30",
                boundDate = "2026-04-25"
            })
        });

        using var verifyContext = new IntegrationDbContext(_options);
        var eventLog = new DomainEventLog(verifyContext);

        var quoteEvents = eventLog.GetByAggregate(DomainEventAggregateKind.Quote, "QT-EVT-001");
        var policyEvents = eventLog.GetByAggregate(DomainEventAggregateKind.Policy, "POL-EVT-001");

        Assert.Multiple(() =>
        {
            // Three envelopes that touched the quote -> three quote events.
            Assert.That(quoteEvents, Has.Count.EqualTo(3));
            Assert.That(quoteEvents.Select(e => e.EventType), Is.EqualTo(new[]
            {
                DomainEventType.RiskSubmissionReceived,
                DomainEventType.QuoteIssued,
                DomainEventType.QuoteBound
            }));
            Assert.That(quoteEvents.Select(e => e.Source), Is.EqualTo(new[] { "CONTOSO_UW", "QUOTEFORGE", "BINDPOINT" }));
            Assert.That(quoteEvents.Select(e => e.CorrelationId), Is.EqualTo(new[] { "corr-evt-1", "corr-evt-2", "corr-evt-3" }));

            // Only the bind envelope produced a policy aggregate event.
            Assert.That(policyEvents, Has.Count.EqualTo(1));
            Assert.That(policyEvents[0].EventType, Is.EqualTo(DomainEventType.PolicyBound));
            Assert.That(policyEvents[0].Source, Is.EqualTo("BINDPOINT"));
            Assert.That(policyEvents[0].EnvelopeId, Is.EqualTo("evt-bp-evt-001"));
            Assert.That(policyEvents[0].PayloadJson, Is.Not.Empty);
        });
    }

    [Test]
    public void IdempotentReplay_DoesNotDoubleAppendHistory()
    {
        var (_, dispatcher) = BuildPipeline();

        var envelope = new SourceIngestEnvelope
        {
            Id = "evt-replay-001",
            Source = "CONTOSO_UW",
            Type = "RiskSubmission",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 25, 3, 0, 0, DateTimeKind.Utc),
            Data = JsonSerializer.SerializeToElement(new
            {
                quoteId = "QT-REPLAY",
                insuredName = "Replay Co",
                trade = "CommercialProperty",
                estimatedPremium = 1000m
            })
        };

        dispatcher.Dispatch(envelope);
        dispatcher.Dispatch(envelope);
        dispatcher.Dispatch(envelope);

        using var verifyContext = new IntegrationDbContext(_options);
        var quoteService = new QuoteSnapshotService(verifyContext, new QuoteSnapshotProjector());
        var quote = quoteService.Find("QT-REPLAY");
        var eventLog = new DomainEventLog(verifyContext);
        var quoteEvents = eventLog.GetByAggregate(DomainEventAggregateKind.Quote, "QT-REPLAY");

        Assert.Multiple(() =>
        {
            Assert.That(quote, Is.Not.Null);
            Assert.That(quote!.History, Has.Count.EqualTo(1), "Idempotent replays must not double-append history");
            Assert.That(quoteEvents, Has.Count.EqualTo(1), "Idempotent replays must not double-write domain events");
        });
    }

    private (RiskIngestHandler handler, IngestDispatcher dispatcher) BuildPipeline()
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

        var handler = new RiskIngestHandler(riskIngestMapper, riskFlowService, router, TimeProvider.System);
        var idempotency = new EfCoreIdempotencyStore(context, TimeProvider.System);
        var dispatcher = new IngestDispatcher(new IIngestHandler[] { handler }, idempotency, TimeProvider.System);
        return (handler, dispatcher);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
