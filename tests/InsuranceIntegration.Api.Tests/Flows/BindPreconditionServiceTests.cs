using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Events;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Correlation;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Services.Matching;
using InsuranceIntegration.Api.Services.Orchestration;
using InsuranceIntegration.Api.Services.Outbox;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using System.Text.Json;

namespace InsuranceIntegration.Api.Tests.Flows;

public sealed class BindPreconditionServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IntegrationDbContext> _options;

    public BindPreconditionServiceTests()
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
    public void Evaluate_NonBindTransaction_PassesWithoutQueryingQuote()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var context = new IntegrationDbContext(_options);
        var quoteService = new QuoteSnapshotService(context, new QuoteSnapshotProjector());
        var service = new BindPreconditionService(quoteService, time);

        var result = service.Evaluate(BuildBindRequest("QT-NOT-CHECKED", "POL-1", transactionType: "Submission"));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Evaluate_BindWithoutQuoteReference_Rejects()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var context = new IntegrationDbContext(_options);
        var quoteService = new QuoteSnapshotService(context, new QuoteSnapshotProjector());
        var service = new BindPreconditionService(quoteService, time);

        var result = service.Evaluate(BuildBindRequest(quoteReference: null, policyReference: "POL-1"));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("missing a quote reference"));
        });
    }

    [Test]
    public void Evaluate_BindAgainstUnknownQuote_Rejects()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var context = new IntegrationDbContext(_options);
        var quoteService = new QuoteSnapshotService(context, new QuoteSnapshotProjector());
        var service = new BindPreconditionService(quoteService, time);

        var result = service.Evaluate(BuildBindRequest("QT-DOES-NOT-EXIST", "POL-1"));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("No quote found"));
        });
    }

    [Test]
    public void Evaluate_BindAgainstExpiredQuote_Rejects()
    {
        // Seed a quote at t0 with the default 30-day validity, then advance the clock
        // past the validity window before checking the precondition.
        var t0 = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var time = new FakeTimeProvider(t0);
        SeedQuote("QT-EXPIRED", t0.UtcDateTime);

        time.Advance(TimeSpan.FromDays(45));

        using var verifyContext = new IntegrationDbContext(_options);
        var quoteService = new QuoteSnapshotService(verifyContext, new QuoteSnapshotProjector());
        var service = new BindPreconditionService(quoteService, time);

        var result = service.Evaluate(BuildBindRequest("QT-EXPIRED", "POL-NEW"));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("expired"));
            Assert.That(result.QuoteVersion, Is.EqualTo(2), "Contoso RiskSubmission + QuoteForge issuance = 2 issuances on the quote");
        });
    }

    [Test]
    public void Evaluate_BindAgainstAlreadyBoundQuoteForDifferentPolicy_Rejects()
    {
        var t0 = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var time = new FakeTimeProvider(t0);
        SeedQuote("QT-BOUND", t0.UtcDateTime);
        SeedBind("QT-BOUND", "POL-FIRST", t0.UtcDateTime.AddMinutes(5));

        using var verifyContext = new IntegrationDbContext(_options);
        var quoteService = new QuoteSnapshotService(verifyContext, new QuoteSnapshotProjector());
        var service = new BindPreconditionService(quoteService, time);

        var result = service.Evaluate(BuildBindRequest("QT-BOUND", "POL-SECOND"));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("already bound to policy 'POL-FIRST'"));
        });
    }

    [Test]
    public void Evaluate_BindAgainstFreshQuotedQuote_Passes()
    {
        var t0 = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var time = new FakeTimeProvider(t0);
        SeedQuote("QT-OK", t0.UtcDateTime);

        time.Advance(TimeSpan.FromDays(5));

        using var verifyContext = new IntegrationDbContext(_options);
        var quoteService = new QuoteSnapshotService(verifyContext, new QuoteSnapshotProjector());
        var service = new BindPreconditionService(quoteService, time);

        var result = service.Evaluate(BuildBindRequest("QT-OK", "POL-OK"));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.QuoteVersion, Is.EqualTo(2));
            Assert.That(result.QuoteValidUntilUtc, Is.GreaterThan(time.GetUtcNow().UtcDateTime));
        });
    }

    private void SeedQuote(string quoteReference, DateTime issuedAtUtc)
    {
        var (handler, dispatcher) = BuildPipeline(issuedAtUtc);
        dispatcher.DispatchAsync(new SourceIngestEnvelope
        {
            Id = $"evt-seed-quote-{quoteReference}",
            Source = "CONTOSO_UW",
            Type = "RiskSubmission",
            SchemaVersion = "1.0",
            OccurredAtUtc = issuedAtUtc,
            Data = JsonSerializer.SerializeToElement(new
            {
                quoteId = quoteReference,
                insuredName = "Precondition Co",
                trade = "CommercialProperty",
                estimatedPremium = 5000m
            })
        }).GetAwaiter().GetResult();

        // Issue a Quoted-status quote so the precondition's status check passes for the
        // happy-path test case.
        dispatcher.DispatchAsync(new SourceIngestEnvelope
        {
            Id = $"evt-seed-issue-{quoteReference}",
            Source = "QUOTEFORGE",
            Type = "QuoteRequest",
            SchemaVersion = "1.0",
            OccurredAtUtc = issuedAtUtc.AddMinutes(1),
            Data = JsonSerializer.SerializeToElement(new
            {
                quoteReference,
                insuredName = "Precondition Co",
                productLine = "COMMERCIAL_PROPERTY",
                brokerCode = "BRK-PC",
                brokerName = "Precondition Brokers",
                technicalPremium = 5000m,
                brokerPremium = 5500m,
                currencyCode = "USD",
                effectiveDate = "2026-05-01",
                expiryDate = "2027-04-30",
                underwritingYear = 2026,
                insuredRevenue = 1_000_000m,
                insuredEmployeeCount = 10,
                insuredYearsInBusiness = 6
            })
        }).GetAwaiter().GetResult();
    }

    private void SeedBind(string quoteReference, string policyReference, DateTime atUtc)
    {
        var (_, dispatcher) = BuildPipeline(atUtc);
        dispatcher.DispatchAsync(new SourceIngestEnvelope
        {
            Id = $"evt-seed-bind-{policyReference}",
            Source = "BINDPOINT",
            Type = "PolicyBindRequest",
            SchemaVersion = "1.0",
            OccurredAtUtc = atUtc,
            Data = JsonSerializer.SerializeToElement(new
            {
                policyReference,
                quoteReference,
                insuredName = "Precondition Co",
                productCode = "COMMERCIAL_PROPERTY",
                brokerCode = "BRK-PC",
                brokerName = "Precondition Brokers",
                brokerHasDelegatedAuthority = true,
                brokerIsPreferredPartner = true,
                boundPremium = 5500m,
                currencyCode = "USD",
                inceptionDate = "2026-05-01",
                expiryDate = "2027-04-30",
                boundDate = "2026-04-25"
            })
        }).GetAwaiter().GetResult();
    }

    private (object handler, IIngestDispatcher dispatcher) BuildPipeline(DateTime nowUtc)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(nowUtc, TimeSpan.Zero));
        var context = new IntegrationDbContext(_options);
        var calculator = new LevenshteinDistanceCalculator();
        var registry = new EfCoreSubmissionRegistry(context, time);
        var clearanceService = new SubmissionClearanceService(registry, calculator);
        var quoteSnapshotService = new QuoteSnapshotService(context, new QuoteSnapshotProjector());
        var bindPrecondition = new BindPreconditionService(quoteSnapshotService, time);
        var riskFlowService = new RiskFlowService(clearanceService, registry, bindPrecondition);
        var riskIngestMapper = new RiskIngestMapper(new ISourceRiskMapper[]
        {
            new ContosoRiskMapper(),
            new QuoteForgeRiskMapper(),
            new BindPointRiskMapper()
        });

        var policyService = new PolicySnapshotService(context, new PolicySnapshotProjector());
        var eventLog = new DomainEventLog(context);
        var router = new RiskSnapshotRouter(policyService, quoteSnapshotService, eventLog, time);
        var correlationContext = new CorrelationContext();
        var outboxWriter = new OutboxWriter(context, correlationContext, time);
        var orchestrator = new RiskSubmissionOrchestrator(riskFlowService, context, outboxWriter, router, time);
        var handler = new RiskIngestHandler(riskIngestMapper, orchestrator, time);
        var idempotency = new EfCoreIdempotencyStore(context, time);
        var dispatcher = new IngestDispatcher(new IIngestHandler[] { handler }, idempotency, time);
        return (handler, dispatcher);
    }

    private static CanonicalRiskRequest BuildBindRequest(string? quoteReference, string policyReference, string transactionType = "PolicyBind")
    {
        return new CanonicalRiskRequest
        {
            EntityId = Guid.NewGuid(),
            ExternalReference = policyReference,
            ProductCode = "COMMERCIAL_PROPERTY",
            SourceSystem = "BINDPOINT",
            TransactionType = transactionType,
            Submission = new SubmissionData { UnderwritingYear = 2026 },
            Broker = new BrokerData { HasDelegatedAuthority = true },
            Insured = new InsuredData { FullName = "Precondition Co" },
            Quote = new QuoteData { QuoteReference = quoteReference },
            Policy = new PolicyData { PolicyReference = policyReference }
        };
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
