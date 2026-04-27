using InsuranceIntegration.Api.CanonicalContracts.Risks;
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

namespace InsuranceIntegration.Api.Tests.Policies;

public sealed class PolicyRenewalServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IntegrationDbContext> _options;

    public PolicyRenewalServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var context = new IntegrationDbContext(_options);
        context.Database.EnsureCreated();
    }

    [TestCase(0.0, 10000.0, "Excellent", -0.05, 9500.00)]
    [TestCase(2000.0, 10000.0, "Excellent", -0.05, 9500.00)]
    [TestCase(4500.0, 10000.0, "Standard", 0.00, 10000.00)]
    [TestCase(7000.0, 10000.0, "Loaded", 0.10, 11000.00)]
    [TestCase(9000.0, 10000.0, "HeavilyLoaded", 0.25, 12500.00)]
    [TestCase(15000.0, 10000.0, "Distressed", 0.40, 14000.00)]
    public void ApplyRenewal_AppliesLossRatioBandsToRenewalPremium(
        double priorClaimsPaidDouble,
        double priorAnnualPremiumDouble,
        string expectedBand,
        double expectedLoadDouble,
        double expectedPremiumDouble)
    {
        var priorClaimsPaid = (decimal)priorClaimsPaidDouble;
        var priorAnnualPremium = (decimal)priorAnnualPremiumDouble;

        var policyReference = $"POL-LR-{(int)priorClaimsPaid}";
        SeedBoundPolicy(policyReference, $"QT-LR-{(int)priorClaimsPaid}");

        var (renewal, _, _) = BuildServices();
        var result = renewal.ApplyRenewal(new RenewalRequest
        {
            PolicyReference = policyReference,
            NewQuoteReference = $"QT-RENEWAL-{(int)priorClaimsPaid}",
            NewInceptionDate = new DateOnly(2027, 1, 1),
            NewExpiryDate = new DateOnly(2027, 12, 31),
            PriorAnnualPremium = priorAnnualPremium,
            PriorClaimsPaid = priorClaimsPaid,
            RevenueDeltaPercent = 0m
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.LossRatioBand, Is.EqualTo(expectedBand));
            Assert.That(result.LossRatioLoadPercent, Is.EqualTo((decimal)expectedLoadDouble));
            Assert.That(result.RenewalPremium, Is.EqualTo((decimal)expectedPremiumDouble));
        });
    }

    [Test]
    public void ApplyRenewal_AppliesExposureAndOverrideLoadsOnTopOfLossRatio()
    {
        SeedBoundPolicy("POL-EXP-001", "QT-EXP-001");

        var (renewal, _, _) = BuildServices();
        var result = renewal.ApplyRenewal(new RenewalRequest
        {
            PolicyReference = "POL-EXP-001",
            NewQuoteReference = "QT-RENEWAL-EXP-001",
            NewInceptionDate = new DateOnly(2027, 1, 1),
            NewExpiryDate = new DateOnly(2027, 12, 31),
            PriorAnnualPremium = 10000m,
            PriorClaimsPaid = 4500m,    // Standard band, 0% load
            RevenueDeltaPercent = 0.20m, // +20% revenue -> +10% exposure load
            OverrideLoadPercent = 0.05m  // underwriter +5%
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.LossRatioBand, Is.EqualTo("Standard"));
            Assert.That(result.LossRatioLoadPercent, Is.EqualTo(0m));
            Assert.That(result.ExposureLoadPercent, Is.EqualTo(0.10m));
            Assert.That(result.OverrideLoadPercent, Is.EqualTo(0.05m));
            // 10000 * (1 + 0 + 0.10 + 0.05) = 11500
            Assert.That(result.RenewalPremium, Is.EqualTo(11500m));
        });
    }

    [Test]
    public void ApplyRenewal_MarksPriorPolicyAsRenewedAndCreatesNewQuoteSnapshotWithLineage()
    {
        SeedBoundPolicy("POL-LIN-001", "QT-LIN-001");

        var (renewal, _, _) = BuildServices();
        renewal.ApplyRenewal(new RenewalRequest
        {
            PolicyReference = "POL-LIN-001",
            NewQuoteReference = "QT-RENEWAL-LIN-001",
            NewInceptionDate = new DateOnly(2027, 1, 1),
            NewExpiryDate = new DateOnly(2027, 12, 31),
            PriorAnnualPremium = 10000m,
            PriorClaimsPaid = 3000m,
            RevenueDeltaPercent = 0m
        });

        using var verifyContext = new IntegrationDbContext(_options);
        var policyService = new PolicySnapshotService(verifyContext, new PolicySnapshotProjector());
        var quoteService = new QuoteSnapshotService(verifyContext, new QuoteSnapshotProjector());
        var prior = policyService.Find("POL-LIN-001");
        var renewalQuote = quoteService.Find("QT-RENEWAL-LIN-001");
        var eventLog = new DomainEventLog(verifyContext);
        var policyEvents = eventLog.GetByAggregate(DomainEventAggregateKind.Policy, "POL-LIN-001");
        var newQuoteEvents = eventLog.GetByAggregate(DomainEventAggregateKind.Quote, "QT-RENEWAL-LIN-001");

        Assert.Multiple(() =>
        {
            // Prior policy transitions to Renewed.
            Assert.That(prior, Is.Not.Null);
            Assert.That(prior!.Lifecycle.PolicyStatus, Is.EqualTo(PolicyStatusValue.Renewed));
            Assert.That(prior.Lifecycle.CurrentPhase, Is.EqualTo("Renewed"));
            Assert.That(prior.History.Last().TransactionType, Is.EqualTo(PolicyTransactionType.Renewal));

            // New renewal quote exists and links back to the prior policy.
            Assert.That(renewalQuote, Is.Not.Null);
            Assert.That(renewalQuote!.PriorPolicyReference, Is.EqualTo("POL-LIN-001"));
            Assert.That(renewalQuote.Lifecycle.QuoteStatus, Is.EqualTo(QuoteStatusValue.Quoted));
            Assert.That(renewalQuote.Lifecycle.Version, Is.EqualTo(1));
            Assert.That(renewalQuote.Lifecycle.IssuedAtUtc, Is.Not.Null);
            Assert.That(renewalQuote.Lifecycle.ValidUntilUtc, Is.Not.Null);

            // Domain events: one PolicyRenewed on prior, one QuoteIssued on the new quote.
            Assert.That(policyEvents.Select(e => e.EventType), Is.EqualTo(new[]
            {
                DomainEventType.PolicyBound,
                DomainEventType.PolicyRenewed
            }));
            Assert.That(newQuoteEvents, Has.Count.EqualTo(1));
            Assert.That(newQuoteEvents[0].EventType, Is.EqualTo(DomainEventType.QuoteIssued));
            Assert.That(newQuoteEvents[0].Source, Is.EqualTo("internal"));
        });
    }

    [Test]
    public void ApplyRenewal_OnUnknownPolicy_ThrowsKeyNotFound()
    {
        var (renewal, _, _) = BuildServices();
        Assert.Throws<KeyNotFoundException>(() => renewal.ApplyRenewal(new RenewalRequest
        {
            PolicyReference = "POL-DOES-NOT-EXIST",
            NewQuoteReference = "QT-NEW",
            NewInceptionDate = new DateOnly(2027, 1, 1),
            NewExpiryDate = new DateOnly(2027, 12, 31),
            PriorAnnualPremium = 1000m,
            PriorClaimsPaid = 0m
        }));
    }

    [Test]
    public void ApplyRenewal_OnCancelledPolicy_ThrowsArgumentException()
    {
        SeedBoundPolicy("POL-CANCELLED-001", "QT-CANCELLED-001");
        var (renewal, lifecycle, _) = BuildServices();
        lifecycle.ApplyCancellation(new CancellationRequest
        {
            PolicyReference = "POL-CANCELLED-001",
            AnnualPremium = 10000m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            CancellationDate = new DateOnly(2026, 7, 1)
        });

        Assert.Throws<ArgumentException>(() => renewal.ApplyRenewal(new RenewalRequest
        {
            PolicyReference = "POL-CANCELLED-001",
            NewQuoteReference = "QT-NEW",
            NewInceptionDate = new DateOnly(2027, 1, 1),
            NewExpiryDate = new DateOnly(2027, 12, 31),
            PriorAnnualPremium = 10000m,
            PriorClaimsPaid = 1000m
        }));
    }

    [Test]
    public void ApplyRenewal_TwiceForSamePolicy_RejectsTheSecondAttempt()
    {
        SeedBoundPolicy("POL-DBL-RENEW-001", "QT-DBL-RENEW-001");
        var (renewal, _, _) = BuildServices();

        renewal.ApplyRenewal(new RenewalRequest
        {
            PolicyReference = "POL-DBL-RENEW-001",
            NewQuoteReference = "QT-RENEWAL-DBL-001",
            NewInceptionDate = new DateOnly(2027, 1, 1),
            NewExpiryDate = new DateOnly(2027, 12, 31),
            PriorAnnualPremium = 10000m,
            PriorClaimsPaid = 0m
        });

        Assert.Throws<ArgumentException>(() => renewal.ApplyRenewal(new RenewalRequest
        {
            PolicyReference = "POL-DBL-RENEW-001",
            NewQuoteReference = "QT-RENEWAL-DBL-002",
            NewInceptionDate = new DateOnly(2027, 1, 1),
            NewExpiryDate = new DateOnly(2027, 12, 31),
            PriorAnnualPremium = 10000m,
            PriorClaimsPaid = 0m
        }));
    }

    private void SeedBoundPolicy(string policyReference, string quoteReference)
    {
        var (_, _, dispatcher) = BuildServices();

        dispatcher.Dispatch(new SourceIngestEnvelope
        {
            Id = $"evt-seed-pol-{policyReference}",
            Source = "POLARIS_UW",
            Type = "RiskSubmission",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 25, 3, 0, 0, DateTimeKind.Utc),
            Data = JsonSerializer.SerializeToElement(new
            {
                quoteId = quoteReference,
                insuredName = "Renewal Test Co",
                trade = "CommercialProperty",
                estimatedPremium = 10000m
            })
        });

        dispatcher.Dispatch(new SourceIngestEnvelope
        {
            Id = $"evt-seed-bp-{policyReference}",
            Source = "BINDPOINT",
            Type = "PolicyBindRequest",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 25, 3, 10, 0, DateTimeKind.Utc),
            Data = JsonSerializer.SerializeToElement(new
            {
                policyReference,
                quoteReference,
                insuredName = "Renewal Test Co",
                productCode = "COMMERCIAL_PROPERTY",
                brokerCode = "BRK-RN",
                brokerName = "Renewal Brokers",
                brokerHasDelegatedAuthority = true,
                brokerIsPreferredPartner = true,
                boundPremium = 10000m,
                currencyCode = "USD",
                inceptionDate = "2026-01-01",
                expiryDate = "2026-12-31",
                boundDate = "2026-04-25"
            })
        });
    }

    private (IPolicyRenewalService Renewal, IPolicyLifecycleService Lifecycle, IIngestDispatcher Dispatcher) BuildServices()
    {
        var context = new IntegrationDbContext(_options);
        var calculator = new LevenshteinDistanceCalculator();
        var registry = new EfCoreSubmissionRegistry(context, TimeProvider.System);
        var clearanceService = new SubmissionClearanceService(registry, calculator);
        var quoteSnapshotService = new QuoteSnapshotService(context, new QuoteSnapshotProjector());
        var bindPrecondition = new BindPreconditionService(quoteSnapshotService, TimeProvider.System);
        var riskFlowService = new RiskFlowService(clearanceService, registry, bindPrecondition);
        var riskIngestMapper = new RiskIngestMapper(new ISourceRiskMapper[]
        {
            new PolarisRiskMapper(),
            new QuoteForgeRiskMapper(),
            new BindPointRiskMapper()
        });
        var policyService = new PolicySnapshotService(context, new PolicySnapshotProjector());
        var eventLog = new DomainEventLog(context);
        var router = new RiskSnapshotRouter(policyService, quoteSnapshotService, eventLog, TimeProvider.System);
        var adjustment = new PolicyAdjustmentService();
        var lifecycle = new PolicyLifecycleService(adjustment, policyService, riskFlowService, router, context, TimeProvider.System);
        var renewal = new PolicyRenewalService(policyService, riskFlowService, router, context, TimeProvider.System);
        var handler = new RiskIngestHandler(riskIngestMapper, riskFlowService, router, TimeProvider.System);
        var idempotency = new EfCoreIdempotencyStore(context, TimeProvider.System);
        var dispatcher = new IngestDispatcher(new IIngestHandler[] { handler }, idempotency, TimeProvider.System);
        return (renewal, lifecycle, dispatcher);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
