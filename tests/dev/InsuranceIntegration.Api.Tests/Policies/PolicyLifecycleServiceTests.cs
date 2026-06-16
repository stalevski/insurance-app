using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Events;
using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Events;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Services.Orchestration;
using InsuranceIntegration.Api.Services.Outbox;
using InsuranceIntegration.Api.Services.Correlation;
using InsuranceIntegration.Api.Services.Matching;
using InsuranceIntegration.Api.Services.Policies;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InsuranceIntegration.Api.Tests.Policies;

public sealed class PolicyLifecycleServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IntegrationDbContext> _options;

    public PolicyLifecycleServiceTests()
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
    public void ApplyCancellation_UpdatesPolicySnapshotAndWritesPolicyCancelledEvent()
    {
        SeedBoundPolicy("POL-LIFE-001", "QT-LIFE-001");

        var (lifecycle, _) = BuildServices();
        var result = lifecycle.ApplyCancellation(new CancellationRequest
        {
            PolicyReference = "POL-LIFE-001",
            AnnualPremium = 10000m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            CancellationDate = new DateOnly(2026, 7, 1),
            Basis = CancellationBasis.ProRata
        });

        using var verifyContext = new IntegrationDbContext(_options);
        var policyService = new PolicySnapshotService(verifyContext, new PolicySnapshotProjector());
        var snapshot = policyService.Find("POL-LIFE-001");
        var quoteService = new QuoteSnapshotService(verifyContext, new QuoteSnapshotProjector());
        var quoteSnapshot = quoteService.Find("QT-LIFE-001");
        var eventLog = new DomainEventLog(verifyContext);
        var policyEvents = eventLog.GetByAggregate(DomainEventAggregateKind.Policy, "POL-LIFE-001");
        var quoteEvents = eventLog.GetByAggregate(DomainEventAggregateKind.Quote, "QT-LIFE-001");

        Assert.Multiple(() =>
        {
            Assert.That(result.PolicyStatus, Is.EqualTo(PolicyStatusValue.Cancelled));
            Assert.That(result.CurrentPhase, Is.EqualTo("Cancelled"));
            Assert.That(result.DomainEventType, Is.EqualTo(DomainEventType.PolicyCancelled));
            Assert.That(result.DomainEventId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Cancellation, Is.Not.Null);
            Assert.That(result.Cancellation!.EarnedPremium, Is.GreaterThan(0m));

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot!.Lifecycle.PolicyStatus, Is.EqualTo(PolicyStatusValue.Cancelled));
            Assert.That(snapshot.Lifecycle.CurrentPhase, Is.EqualTo("Cancelled"));
            Assert.That(snapshot.Lifecycle.AutoCleared, Is.True, "AutoCleared should remain true once previously achieved");
            Assert.That(snapshot.History.Select(h => h.TransactionType), Does.Contain(PolicyTransactionType.Cancellation));
            Assert.That(snapshot.History.Last().Source, Is.EqualTo("internal"));

            Assert.That(policyEvents.Select(e => e.EventType), Is.EqualTo(new[]
            {
                DomainEventType.PolicyBound,
                DomainEventType.PolicyCancelled
            }));
            Assert.That(quoteEvents.Select(e => e.EventType), Is.EqualTo(new[]
            {
                DomainEventType.RiskSubmissionReceived,
                DomainEventType.QuoteBound
            }), "Cancellation must not write a quote-aggregate event");
            Assert.That(quoteSnapshot, Is.Not.Null);
            Assert.That(quoteSnapshot!.Lifecycle.CurrentPhase, Is.EqualTo("Bound"), "QuoteSnapshot is unaffected by post-bind cancellation");
        });
    }

    [Test]
    public void ApplyEndorsement_UpdatesPolicyToEndorsedAndWritesPolicyEndorsedEvent()
    {
        SeedBoundPolicy("POL-LIFE-002", "QT-LIFE-002");

        var (lifecycle, _) = BuildServices();
        var result = lifecycle.ApplyEndorsement(new EndorsementRequest
        {
            PolicyReference = "POL-LIFE-002",
            CurrentAnnualPremium = 10000m,
            NewAnnualPremium = 12500m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            EffectiveDate = new DateOnly(2026, 6, 1),
            SectionOperations =
            [
                new SectionEndorsementOperation
                {
                    OperationType = "Update",
                    SectionCode = "PROP",
                    SumInsuredDelta = 100000m,
                    PremiumDelta = 2500m
                }
            ]
        });

        using var verifyContext = new IntegrationDbContext(_options);
        var policyService = new PolicySnapshotService(verifyContext, new PolicySnapshotProjector());
        var snapshot = policyService.Find("POL-LIFE-002");
        var eventLog = new DomainEventLog(verifyContext);
        var policyEvents = eventLog.GetByAggregate(DomainEventAggregateKind.Policy, "POL-LIFE-002");

        Assert.Multiple(() =>
        {
            Assert.That(result.PolicyStatus, Is.EqualTo(PolicyStatusValue.Endorsed));
            Assert.That(result.CurrentPhase, Is.EqualTo("Endorsed"));
            Assert.That(result.DomainEventType, Is.EqualTo(DomainEventType.PolicyEndorsed));
            Assert.That(result.Endorsement, Is.Not.Null);
            Assert.That(result.Endorsement!.PremiumDelta, Is.EqualTo(2500m));

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot!.Lifecycle.PolicyStatus, Is.EqualTo(PolicyStatusValue.Endorsed));
            Assert.That(snapshot.Lifecycle.CurrentPhase, Is.EqualTo("Endorsed"));

            Assert.That(policyEvents.Select(e => e.EventType), Is.EqualTo(new[]
            {
                DomainEventType.PolicyBound,
                DomainEventType.PolicyEndorsed
            }));
        });
    }

    [Test]
    public void ApplyCancellation_OnUnknownPolicy_ThrowsKeyNotFound()
    {
        var (lifecycle, _) = BuildServices();
        Assert.Throws<KeyNotFoundException>(() => lifecycle.ApplyCancellation(new CancellationRequest
        {
            PolicyReference = "POL-DOES-NOT-EXIST",
            AnnualPremium = 1000m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            CancellationDate = new DateOnly(2026, 7, 1)
        }));
    }

    [Test]
    public void ApplyReinstatement_OnCancelledPolicy_RestoresPolicyAndWritesPolicyReinstatedEvent()
    {
        SeedBoundPolicy("POL-LIFE-003", "QT-LIFE-003");

        var (lifecycle, _) = BuildServices();
        lifecycle.ApplyCancellation(new CancellationRequest
        {
            PolicyReference = "POL-LIFE-003",
            AnnualPremium = 10000m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            CancellationDate = new DateOnly(2026, 4, 1),
            Basis = CancellationBasis.ProRata
        });

        var result = lifecycle.ApplyReinstatement(new ReinstatementRequest
        {
            PolicyReference = "POL-LIFE-003",
            AnnualPremium = 10000m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            CancellationDate = new DateOnly(2026, 4, 1),
            ReinstatementDate = new DateOnly(2026, 5, 1),
            ReinstatementFee = 75m,
            ChargeLapsedPremium = true,
            Reason = "Payment received"
        });

        using var verifyContext = new IntegrationDbContext(_options);
        var policyService = new PolicySnapshotService(verifyContext, new PolicySnapshotProjector());
        var snapshot = policyService.Find("POL-LIFE-003");
        var eventLog = new DomainEventLog(verifyContext);
        var policyEvents = eventLog.GetByAggregate(DomainEventAggregateKind.Policy, "POL-LIFE-003");

        Assert.Multiple(() =>
        {
            Assert.That(result.PolicyStatus, Is.EqualTo(PolicyStatusValue.Reinstated));
            Assert.That(result.CurrentPhase, Is.EqualTo("Reinstated"));
            Assert.That(result.DomainEventType, Is.EqualTo(DomainEventType.PolicyReinstated));
            Assert.That(result.DomainEventId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Reinstatement, Is.Not.Null);
            Assert.That(result.Reinstatement!.AmountDueOnReinstatement, Is.GreaterThanOrEqualTo(75m));

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot!.Lifecycle.PolicyStatus, Is.EqualTo(PolicyStatusValue.Reinstated));
            Assert.That(snapshot.Lifecycle.CurrentPhase, Is.EqualTo("Reinstated"));
            Assert.That(snapshot.History.Select(h => h.TransactionType), Does.Contain(PolicyTransactionType.Reinstatement));

            Assert.That(policyEvents.Select(e => e.EventType), Is.EqualTo(new[]
            {
                DomainEventType.PolicyBound,
                DomainEventType.PolicyCancelled,
                DomainEventType.PolicyReinstated
            }));
        });
    }

    [Test]
    public void ApplyReinstatement_OnActivePolicy_ThrowsArgumentException()
    {
        SeedBoundPolicy("POL-LIFE-004", "QT-LIFE-004");

        var (lifecycle, _) = BuildServices();
        Assert.Throws<ArgumentException>(() => lifecycle.ApplyReinstatement(new ReinstatementRequest
        {
            PolicyReference = "POL-LIFE-004",
            AnnualPremium = 10000m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            CancellationDate = new DateOnly(2026, 4, 1),
            ReinstatementDate = new DateOnly(2026, 5, 1)
        }));
    }

    [Test]
    public void ApplyReinstatement_OnUnknownPolicy_ThrowsKeyNotFound()
    {
        var (lifecycle, _) = BuildServices();
        Assert.Throws<KeyNotFoundException>(() => lifecycle.ApplyReinstatement(new ReinstatementRequest
        {
            PolicyReference = "POL-DOES-NOT-EXIST",
            AnnualPremium = 1000m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            CancellationDate = new DateOnly(2026, 4, 1),
            ReinstatementDate = new DateOnly(2026, 5, 1)
        }));
    }

    [Test]
    public void ApplyLapse_OnInForcePolicy_LapsesPolicyAndWritesPolicyLapsedEvent()
    {
        SeedBoundPolicy("POL-LIFE-005", "QT-LIFE-005");

        var (lifecycle, _) = BuildServices();
        var result = lifecycle.ApplyLapse(new LapseRequest
        {
            PolicyReference = "POL-LIFE-005",
            AnnualPremium = 10000m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            LapseDate = new DateOnly(2026, 7, 1),
            PaidToDate = 2500m,
            Reason = "Premium installment unpaid past grace period"
        });

        using var verifyContext = new IntegrationDbContext(_options);
        var policyService = new PolicySnapshotService(verifyContext, new PolicySnapshotProjector());
        var snapshot = policyService.Find("POL-LIFE-005");
        var eventLog = new DomainEventLog(verifyContext);
        var policyEvents = eventLog.GetByAggregate(DomainEventAggregateKind.Policy, "POL-LIFE-005");

        Assert.Multiple(() =>
        {
            Assert.That(result.PolicyStatus, Is.EqualTo(PolicyStatusValue.Lapsed));
            Assert.That(result.CurrentPhase, Is.EqualTo("Lapsed"));
            Assert.That(result.DomainEventType, Is.EqualTo(DomainEventType.PolicyLapsed));
            Assert.That(result.DomainEventId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Lapse, Is.Not.Null);
            Assert.That(result.Lapse!.CoveredDays, Is.EqualTo(181));
            Assert.That(result.Lapse.EarnedPremium, Is.GreaterThan(0m));
            Assert.That(result.Lapse.OutstandingPremium, Is.EqualTo(result.Lapse.EarnedPremium - 2500m));

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot!.Lifecycle.PolicyStatus, Is.EqualTo(PolicyStatusValue.Lapsed));
            Assert.That(snapshot.Lifecycle.CurrentPhase, Is.EqualTo("Lapsed"));
            Assert.That(snapshot.History.Select(h => h.TransactionType), Does.Contain(PolicyTransactionType.Lapse));

            Assert.That(policyEvents.Select(e => e.EventType), Is.EqualTo(new[]
            {
                DomainEventType.PolicyBound,
                DomainEventType.PolicyLapsed
            }));
        });
    }

    [Test]
    public void ApplyLapse_OnUnknownPolicy_ThrowsKeyNotFound()
    {
        var (lifecycle, _) = BuildServices();
        Assert.Throws<KeyNotFoundException>(() => lifecycle.ApplyLapse(new LapseRequest
        {
            PolicyReference = "POL-DOES-NOT-EXIST",
            AnnualPremium = 1000m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            LapseDate = new DateOnly(2026, 7, 1)
        }));
    }

    [Test]
    public void ApplyNonRenewal_OnInForcePolicy_MarksNonRenewedAndWritesPolicyNonRenewedEvent()
    {
        SeedBoundPolicy("POL-LIFE-006", "QT-LIFE-006");

        var (lifecycle, _) = BuildServices();
        var result = lifecycle.ApplyNonRenewal(new NonRenewalRequest
        {
            PolicyReference = "POL-LIFE-006",
            AnnualPremium = 10000m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            InitiatedBy = NonRenewalInitiator.Insurer,
            NoticeDays = 30,
            Reason = "Risk appetite change"
        });

        using var verifyContext = new IntegrationDbContext(_options);
        var policyService = new PolicySnapshotService(verifyContext, new PolicySnapshotProjector());
        var snapshot = policyService.Find("POL-LIFE-006");
        var eventLog = new DomainEventLog(verifyContext);
        var policyEvents = eventLog.GetByAggregate(DomainEventAggregateKind.Policy, "POL-LIFE-006");

        Assert.Multiple(() =>
        {
            Assert.That(result.PolicyStatus, Is.EqualTo(PolicyStatusValue.NonRenewed));
            Assert.That(result.CurrentPhase, Is.EqualTo("NonRenewed"));
            Assert.That(result.DomainEventType, Is.EqualTo(DomainEventType.PolicyNonRenewed));
            Assert.That(result.DomainEventId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.NonRenewal, Is.Not.Null);
            Assert.That(result.NonRenewal!.EffectiveDate, Is.EqualTo(new DateOnly(2026, 12, 31)));
            Assert.That(result.NonRenewal.InitiatedBy, Is.EqualTo(NonRenewalInitiator.Insurer));
            Assert.That(result.NonRenewal.NoticeDays, Is.EqualTo(30));

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot!.Lifecycle.PolicyStatus, Is.EqualTo(PolicyStatusValue.NonRenewed));
            Assert.That(snapshot.Lifecycle.CurrentPhase, Is.EqualTo("NonRenewed"));
            Assert.That(snapshot.History.Select(h => h.TransactionType), Does.Contain(PolicyTransactionType.NonRenewal));

            Assert.That(policyEvents.Select(e => e.EventType), Is.EqualTo(new[]
            {
                DomainEventType.PolicyBound,
                DomainEventType.PolicyNonRenewed
            }));
        });
    }

    [Test]
    public void ApplyNonRenewal_OnCancelledPolicy_ThrowsArgumentException()
    {
        SeedBoundPolicy("POL-LIFE-007", "QT-LIFE-007");

        var (lifecycle, _) = BuildServices();
        lifecycle.ApplyCancellation(new CancellationRequest
        {
            PolicyReference = "POL-LIFE-007",
            AnnualPremium = 10000m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31),
            CancellationDate = new DateOnly(2026, 4, 1),
            Basis = CancellationBasis.ProRata
        });

        Assert.Throws<ArgumentException>(() => lifecycle.ApplyNonRenewal(new NonRenewalRequest
        {
            PolicyReference = "POL-LIFE-007",
            AnnualPremium = 10000m,
            InceptionDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31)
        }));
    }

    private void SeedBoundPolicy(string policyReference, string quoteReference)
    {
        var (_, dispatcher) = BuildServices();

        dispatcher.DispatchAsync(new SourceIngestEnvelope
        {
            Id = $"evt-seed-pol-{policyReference}",
            Source = "CONTOSO_UW",
            Type = "RiskSubmission",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 25, 3, 0, 0, DateTimeKind.Utc),
            Data = JsonSerializer.SerializeToElement(new
            {
                quoteId = quoteReference,
                insuredName = "Lifecycle Test Co",
                trade = "CommercialProperty",
                estimatedPremium = 10000m
            })
        }).GetAwaiter().GetResult();

        dispatcher.DispatchAsync(new SourceIngestEnvelope
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
                insuredName = "Lifecycle Test Co",
                productCode = "COMMERCIAL_PROPERTY",
                brokerCode = "BRK-LIFE",
                brokerName = "Lifecycle Brokers",
                brokerHasDelegatedAuthority = true,
                brokerIsPreferredPartner = true,
                boundPremium = 10000m,
                currencyCode = "USD",
                inceptionDate = "2026-01-01",
                expiryDate = "2026-12-31",
                boundDate = "2026-04-25"
            })
        }).GetAwaiter().GetResult();
    }

    private (IPolicyLifecycleService Lifecycle, IIngestDispatcher Dispatcher) BuildServices()
    {
        var context = new IntegrationDbContext(_options);
        var calculator = new LevenshteinDistanceCalculator();
        var registry = new EfCoreSubmissionRegistry(context, TimeProvider.System);
        var clearanceService = new SubmissionClearanceService(registry, calculator);
        var riskFlowService = new RiskFlowService(clearanceService, registry);
        var riskIngestMapper = new RiskIngestMapper(new ISourceRiskMapper[]
        {
            new ContosoRiskMapper(TimeProvider.System),
            new QuoteForgeRiskMapper(TimeProvider.System),
            new BindPointRiskMapper(TimeProvider.System)
        });

        var policyProjector = new PolicySnapshotProjector();
        var quoteProjector = new QuoteSnapshotProjector();
        var policyService = new PolicySnapshotService(context, policyProjector);
        var quoteService = new QuoteSnapshotService(context, quoteProjector);
        var eventLog = new DomainEventLog(context);
        var router = new RiskSnapshotRouter(policyService, quoteService, eventLog, TimeProvider.System);
        var adjustmentService = new PolicyAdjustmentService();
        var lifecycle = new PolicyLifecycleService(adjustmentService, policyService, riskFlowService, router, context, TimeProvider.System);
        var correlationContext = new CorrelationContext();
        var outboxWriter = new OutboxWriter(context, correlationContext, TimeProvider.System);
        var orchestrator = new RiskSubmissionOrchestrator(riskFlowService, context, outboxWriter, router, TimeProvider.System);
        var handler = new RiskIngestHandler(riskIngestMapper, orchestrator, TimeProvider.System);
        var idempotency = new EfCoreIdempotencyStore(context, TimeProvider.System);
        var dispatcher = new IngestDispatcher(new IIngestHandler[] { handler }, idempotency, TimeProvider.System);
        return (lifecycle, dispatcher);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
