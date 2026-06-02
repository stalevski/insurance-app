using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Correlation;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Matching;
using InsuranceIntegration.Api.Services.Orchestration;
using InsuranceIntegration.Api.Services.Outbox;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.CanonicalContracts.Risks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InsuranceIntegration.Api.Tests.Orchestration;

public sealed class RiskSubmissionOrchestratorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IntegrationDbContext> _options;

    public RiskSubmissionOrchestratorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new RowVersionInterceptor())
            .Options;

        using var ctx = new IntegrationDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    [Test]
    public async Task HandleAsync_PersistsSubmissionAndQuoteAndEmitsOutboxEvent()
    {
        using var ctx = new IntegrationDbContext(_options);
        var orchestrator = CreateOrchestrator(ctx);

        var request = BuildRequest(externalReference: "EXT-ORCH-001", transactionType: "Submission");
        var context = BuildIngestContext("POLARIS_UW", "evt-1");

        var response = await orchestrator.HandleAsync(request, context);

        Assert.That(response, Is.Not.Null);
        Assert.That(response.ExternalReference, Is.EqualTo("EXT-ORCH-001"));

        using var readCtx = new IntegrationDbContext(_options);

        var submission = await readCtx.Submissions
            .SingleOrDefaultAsync(s => s.ExternalReference == "EXT-ORCH-001");
        Assert.That(submission, Is.Not.Null);
        Assert.That(submission!.ProductCode, Is.EqualTo("COMMERCIAL_PROPERTY"));
        Assert.That(submission.RowVersion, Is.Not.Empty);

        var quote = await readCtx.Quotes
            .SingleOrDefaultAsync(q => q.SubmissionId == submission.Id);
        Assert.That(quote, Is.Not.Null);
        Assert.That(quote!.QuoteReference, Is.EqualTo("Q-EXT-ORCH-001"));

        var outbox = await readCtx.OutboxMessages.ToListAsync();
        Assert.That(outbox.Count, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task HandleAsync_IdempotentUpsert_DoesNotDuplicateSubmission()
    {
        using var ctx1 = new IntegrationDbContext(_options);
        var orchestrator1 = CreateOrchestrator(ctx1);
        var request = BuildRequest(externalReference: "EXT-IDEM-001", transactionType: "Submission");
        var context = BuildIngestContext("POLARIS_UW", "evt-idem-1");

        await orchestrator1.HandleAsync(request, context);

        using var ctx2 = new IntegrationDbContext(_options);
        var orchestrator2 = CreateOrchestrator(ctx2);
        await orchestrator2.HandleAsync(request, context);

        using var readCtx = new IntegrationDbContext(_options);
        var count = await readCtx.Submissions
            .CountAsync(s => s.ExternalReference == "EXT-IDEM-001");

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_PolicyBind_PersistsPolicyEntity()
    {
        using var ctx1 = new IntegrationDbContext(_options);
        var orchestrator1 = CreateOrchestrator(ctx1);
        var subRequest = BuildRequest(externalReference: "EXT-BIND-001", transactionType: "Submission");
        await orchestrator1.HandleAsync(subRequest, BuildIngestContext("POLARIS_UW", "evt-sub-1"));

        using var ctx2 = new IntegrationDbContext(_options);
        var orchestrator2 = CreateOrchestrator(ctx2);
        var bindRequest = BuildRequest(
            externalReference: "EXT-BIND-001",
            transactionType: "PolicyBind",
            policyReference: "POL-2026-001");

        await orchestrator2.HandleAsync(bindRequest, BuildIngestContext("POLARIS_UW", "evt-bind-1"));

        using var readCtx = new IntegrationDbContext(_options);
        var policy = await readCtx.Policies
            .SingleOrDefaultAsync(p => p.PolicyReference == "POL-2026-001");

        Assert.That(policy, Is.Not.Null);
        Assert.That(policy!.ProductCode, Is.EqualTo("COMMERCIAL_PROPERTY"));
        Assert.That(policy.InsuredName, Is.EqualTo("Northwind Storage Ltd"));
        Assert.That(policy.RowVersion, Is.Not.Empty);
    }

    [Test]
    public async Task HandleAsync_SecondCall_UpdatesSubmissionStatus()
    {
        using var ctx1 = new IntegrationDbContext(_options);
        var orchestrator1 = CreateOrchestrator(ctx1);
        await orchestrator1.HandleAsync(
            BuildRequest("EXT-UPD-001", transactionType: "Submission"),
            BuildIngestContext("POLARIS_UW", "evt-upd-1"));

        using var ctx2 = new IntegrationDbContext(_options);
        var orchestrator2 = CreateOrchestrator(ctx2);
        await orchestrator2.HandleAsync(
            BuildRequest("EXT-UPD-001", transactionType: "Submission"),
            BuildIngestContext("POLARIS_UW", "evt-upd-2"));

        using var readCtx = new IntegrationDbContext(_options);
        var count = await readCtx.Submissions
            .CountAsync(s => s.ExternalReference == "EXT-UPD-001");

        Assert.That(count, Is.EqualTo(1));

        var outboxCount = await readCtx.OutboxMessages.CountAsync();
        Assert.That(outboxCount, Is.GreaterThanOrEqualTo(2));
    }

    public void Dispose() => _connection.Dispose();

    private RiskSubmissionOrchestrator CreateOrchestrator(IntegrationDbContext ctx)
    {
        var calculator = new LevenshteinDistanceCalculator();
        var registry = new InMemorySubmissionRegistry();
        var clearanceService = new SubmissionClearanceService(registry, calculator);
        var riskFlowService = new RiskFlowService(clearanceService, registry);
        var correlationContext = new CorrelationContext();
        var outboxWriter = new OutboxWriter(ctx, correlationContext, TimeProvider.System);
        var snapshotRouter = new NoOpSnapshotRouter();
        return new RiskSubmissionOrchestrator(riskFlowService, ctx, outboxWriter, snapshotRouter, TimeProvider.System);
    }

    private static CanonicalRiskRequest BuildRequest(
        string externalReference = "EXT-TEST-001",
        string transactionType = "Submission",
        string? policyReference = null)
    {
        return new CanonicalRiskRequest
        {
            EntityId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ExternalReference = externalReference,
            ProductCode = "COMMERCIAL_PROPERTY",
            SourceSystem = "POLARIS_UW",
            TransactionType = transactionType,
            SchemeCode = "STANDARD",
            TransactionTimestampUtc = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc),
            LifecycleStatus = "Ingested",
            CurrencyCode = "USD",
            Submission = new SubmissionData { UnderwritingYear = 2026, BrokerPremium = 1000m, Revenue = 500000m },
            Broker = new BrokerData { BrokerCode = "BRK-1", BrokerName = "Summit Risk Partners" },
            Insured = new InsuredData { FullName = "Northwind Storage Ltd", TradingName = "Northwind", SegmentCode = "SME", AnnualRevenue = 500000m, YearsInBusiness = 5 },
            Quote = new QuoteData { QuoteReference = $"Q-{externalReference}", EffectiveDate = new DateOnly(2026, 5, 1), ExpiryDate = new DateOnly(2027, 4, 30) },
            Policy = new PolicyData { PolicyReference = policyReference ?? string.Empty, InceptionDate = new DateOnly(2026, 1, 1), ExpiryDate = new DateOnly(2026, 12, 31) },
            Clearance = new ClearanceData { AutoClearanceEnabled = true, PremiumThreshold = 5000m },
            Enrichments = [],
            ContractChecks = [],
            ComplianceChecks = [],
            Parties = [],
            Claims = [],
            Sections = [],
            SectionOperations = [],
            Installments = []
        };
    }

    private static IngestContext BuildIngestContext(string source, string envelopeId)
    {
        return new IngestContext
        {
            Source = source,
            EnvelopeId = envelopeId,
            MessageType = "RiskSubmission",
            ReceivedAtUtc = new DateTime(2026, 4, 24, 8, 0, 0, DateTimeKind.Utc)
        };
    }

    private sealed class NoOpSnapshotRouter : IRiskSnapshotRouter
    {
        public void Route(CanonicalRiskRequest request, global::InsuranceIntegration.Api.Responses.Risks.FinalRiskResponse response, IngestContext context)
        {
        }
    }
}
