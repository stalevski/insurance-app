using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Events;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.Snapshots.Policies;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Services.Policies;

public sealed class PolicyLifecycleService : IPolicyLifecycleService
{
    private const string InternalSourceCode = "internal";

    private readonly IPolicyAdjustmentService _adjustmentService;
    private readonly IPolicySnapshotService _policySnapshotService;
    private readonly IRiskFlowService _riskFlowService;
    private readonly IRiskSnapshotRouter _riskSnapshotRouter;
    private readonly IntegrationDbContext _context;
    private readonly TimeProvider _time;

    public PolicyLifecycleService(
        IPolicyAdjustmentService adjustmentService,
        IPolicySnapshotService policySnapshotService,
        IRiskFlowService riskFlowService,
        IRiskSnapshotRouter riskSnapshotRouter,
        IntegrationDbContext context,
        TimeProvider time)
    {
        _adjustmentService = adjustmentService;
        _policySnapshotService = policySnapshotService;
        _riskFlowService = riskFlowService;
        _riskSnapshotRouter = riskSnapshotRouter;
        _context = context;
        _time = time;
    }

    public PolicyLifecycleResult ApplyCancellation(CancellationRequest request)
    {
        var math = _adjustmentService.CalculateCancellation(request);
        var snapshot = LoadSnapshotOrThrow(request.PolicyReference);
        var nowUtc = _time.GetUtcNow().UtcDateTime;

        var canonical = SynthesizeCanonical(
            snapshot,
            transactionType: PolicyTransactionType.Cancellation,
            transactionTimestampUtc: nowUtc,
            // Cancel keeps the original annual premium so the projector preserves figures;
            // the financial math (return premium, retained premium, etc.) lives on the result.
            brokerPremium: request.AnnualPremium,
            inceptionDate: request.InceptionDate,
            expiryDate: request.ExpiryDate,
            sectionOperations: []);

        var context = BuildInternalContext(
            request.PolicyReference,
            messageType: "PolicyCancellation",
            occurredAtUtc: nowUtc);

        var (response, eventId) = RouteAndCaptureEventId(canonical, context);

        var refreshed = _policySnapshotService.Find(request.PolicyReference) ?? snapshot;

        return new PolicyLifecycleResult
        {
            PolicyReference = request.PolicyReference,
            TransactionType = PolicyTransactionType.Cancellation,
            PolicyStatus = response.PolicyStatus,
            CurrentPhase = refreshed.Lifecycle.CurrentPhase,
            DomainEventId = eventId,
            DomainEventType = DomainEventType.PolicyCancelled,
            Cancellation = math
        };
    }

    public PolicyLifecycleResult ApplyEndorsement(EndorsementRequest request)
    {
        var math = _adjustmentService.CalculateEndorsement(request);
        var snapshot = LoadSnapshotOrThrow(request.PolicyReference);
        var nowUtc = _time.GetUtcNow().UtcDateTime;

        var sectionOperations = request.SectionOperations
            .Select(operation => new SectionOperation
            {
                SectionCode = operation.SectionCode,
                OperationType = string.IsNullOrWhiteSpace(operation.OperationType) ? "Update" : operation.OperationType,
                SubcoverCode = operation.SubcoverCode,
                RemoveAllSubcovers = false
            })
            .ToList();

        var canonical = SynthesizeCanonical(
            snapshot,
            transactionType: PolicyTransactionType.MidTermAdjustment,
            transactionTimestampUtc: nowUtc,
            brokerPremium: request.NewAnnualPremium,
            inceptionDate: request.InceptionDate,
            expiryDate: request.ExpiryDate,
            sectionOperations: sectionOperations);

        var context = BuildInternalContext(
            request.PolicyReference,
            messageType: "PolicyEndorsement",
            occurredAtUtc: nowUtc);

        var (response, eventId) = RouteAndCaptureEventId(canonical, context);

        var refreshed = _policySnapshotService.Find(request.PolicyReference) ?? snapshot;

        return new PolicyLifecycleResult
        {
            PolicyReference = request.PolicyReference,
            TransactionType = PolicyTransactionType.MidTermAdjustment,
            PolicyStatus = response.PolicyStatus,
            CurrentPhase = refreshed.Lifecycle.CurrentPhase,
            DomainEventId = eventId,
            DomainEventType = DomainEventType.PolicyEndorsed,
            Endorsement = math
        };
    }

    public PolicyLifecycleResult ApplyReinstatement(ReinstatementRequest request)
    {
        var snapshot = LoadSnapshotOrThrow(request.PolicyReference);

        if (!string.Equals(snapshot.Lifecycle.PolicyStatus, PolicyStatusValue.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Policy '{request.PolicyReference}' is '{snapshot.Lifecycle.PolicyStatus}'. Only a cancelled policy can be reinstated.");
        }

        var math = _adjustmentService.CalculateReinstatement(request);
        var nowUtc = _time.GetUtcNow().UtcDateTime;

        var canonical = SynthesizeCanonical(
            snapshot,
            transactionType: PolicyTransactionType.Reinstatement,
            transactionTimestampUtc: nowUtc,
            brokerPremium: math.ReinstatedAnnualPremium,
            inceptionDate: request.InceptionDate,
            expiryDate: request.ExpiryDate,
            sectionOperations: []);

        var context = BuildInternalContext(
            request.PolicyReference,
            messageType: "PolicyReinstatement",
            occurredAtUtc: nowUtc);

        var (response, eventId) = RouteAndCaptureEventId(canonical, context);

        var refreshed = _policySnapshotService.Find(request.PolicyReference) ?? snapshot;

        return new PolicyLifecycleResult
        {
            PolicyReference = request.PolicyReference,
            TransactionType = PolicyTransactionType.Reinstatement,
            PolicyStatus = response.PolicyStatus,
            CurrentPhase = refreshed.Lifecycle.CurrentPhase,
            DomainEventId = eventId,
            DomainEventType = DomainEventType.PolicyReinstated,
            Reinstatement = math
        };
    }

    public PolicyLifecycleResult ApplyLapse(LapseRequest request)
    {
        var snapshot = LoadSnapshotOrThrow(request.PolicyReference);

        if (!IsInForce(snapshot.Lifecycle.PolicyStatus))
        {
            throw new ArgumentException(
                $"Policy '{request.PolicyReference}' is '{snapshot.Lifecycle.PolicyStatus}'. Only an in-force policy can lapse.");
        }

        var math = _adjustmentService.CalculateLapse(request);
        var nowUtc = _time.GetUtcNow().UtcDateTime;

        var canonical = SynthesizeCanonical(
            snapshot,
            transactionType: PolicyTransactionType.Lapse,
            transactionTimestampUtc: nowUtc,
            // Lapse keeps the annual premium on the snapshot; the earned/outstanding math
            // lives on the result, not the snapshot premium figures.
            brokerPremium: request.AnnualPremium,
            inceptionDate: request.InceptionDate,
            expiryDate: request.ExpiryDate,
            sectionOperations: []);

        var context = BuildInternalContext(
            request.PolicyReference,
            messageType: "PolicyLapse",
            occurredAtUtc: nowUtc);

        var (response, eventId) = RouteAndCaptureEventId(canonical, context);

        var refreshed = _policySnapshotService.Find(request.PolicyReference) ?? snapshot;

        return new PolicyLifecycleResult
        {
            PolicyReference = request.PolicyReference,
            TransactionType = PolicyTransactionType.Lapse,
            PolicyStatus = response.PolicyStatus,
            CurrentPhase = refreshed.Lifecycle.CurrentPhase,
            DomainEventId = eventId,
            DomainEventType = DomainEventType.PolicyLapsed,
            Lapse = math
        };
    }

    public PolicyLifecycleResult ApplyNonRenewal(NonRenewalRequest request)
    {
        var snapshot = LoadSnapshotOrThrow(request.PolicyReference);

        if (!IsInForce(snapshot.Lifecycle.PolicyStatus))
        {
            throw new ArgumentException(
                $"Policy '{request.PolicyReference}' is '{snapshot.Lifecycle.PolicyStatus}'. Only an in-force policy can be non-renewed.");
        }

        var math = _adjustmentService.CalculateNonRenewal(request);
        var nowUtc = _time.GetUtcNow().UtcDateTime;

        var canonical = SynthesizeCanonical(
            snapshot,
            transactionType: PolicyTransactionType.NonRenewal,
            transactionTimestampUtc: nowUtc,
            brokerPremium: request.AnnualPremium,
            inceptionDate: request.InceptionDate,
            expiryDate: request.ExpiryDate,
            sectionOperations: []);

        var context = BuildInternalContext(
            request.PolicyReference,
            messageType: "PolicyNonRenewal",
            occurredAtUtc: nowUtc);

        var (response, eventId) = RouteAndCaptureEventId(canonical, context);

        var refreshed = _policySnapshotService.Find(request.PolicyReference) ?? snapshot;

        return new PolicyLifecycleResult
        {
            PolicyReference = request.PolicyReference,
            TransactionType = PolicyTransactionType.NonRenewal,
            PolicyStatus = response.PolicyStatus,
            CurrentPhase = refreshed.Lifecycle.CurrentPhase,
            DomainEventId = eventId,
            DomainEventType = DomainEventType.PolicyNonRenewed,
            NonRenewal = math
        };
    }

    private static readonly string[] InForceStatuses =
    [
        PolicyStatusValue.Bound,
        PolicyStatusValue.Endorsed,
        PolicyStatusValue.Renewed,
        PolicyStatusValue.Reinstated
    ];

    private static bool IsInForce(string policyStatus)
    {
        return InForceStatuses.Any(status => string.Equals(status, policyStatus, StringComparison.OrdinalIgnoreCase));
    }

    private PolicySnapshot LoadSnapshotOrThrow(string policyReference)
    {
        var snapshot = _policySnapshotService.Find(policyReference);
        if (snapshot is null)
        {
            throw new KeyNotFoundException($"Policy '{policyReference}' was not found. The policy must be bound before it can be cancelled or endorsed.");
        }
        return snapshot;
    }

    private static IngestContext BuildInternalContext(string policyReference, string messageType, DateTime occurredAtUtc)
    {
        return new IngestContext
        {
            Source = InternalSourceCode,
            EnvelopeId = $"{messageType.ToLowerInvariant()}-{policyReference}-{occurredAtUtc:yyyyMMddHHmmssfff}",
            MessageType = messageType,
            ReceivedAtUtc = occurredAtUtc,
            CorrelationId = null
        };
    }

    private (Responses.Risks.FinalRiskResponse Response, Guid EventId) RouteAndCaptureEventId(
        CanonicalRiskRequest canonical,
        IngestContext context)
    {
        var response = _riskFlowService.Process(canonical);

        // The router queues a DomainEvent on the same DbContext; the snapshot service's
        // SaveChanges flushes both rows in one transaction. We then look up the row by its
        // unique synthetic (Source, EnvelopeId) tuple to capture the assigned id.
        _riskSnapshotRouter.Route(canonical, response, context);

        var eventId = _context.DomainEvents
            .AsNoTracking()
            .Where(record => record.Source == context.Source
                && record.EnvelopeId == context.EnvelopeId
                && record.AggregateKind == DomainEventAggregateKind.Policy)
            .OrderByDescending(record => record.RecordedAtUtc)
            .Select(record => record.Id)
            .FirstOrDefault();

        return (response, eventId);
    }

    private static CanonicalRiskRequest SynthesizeCanonical(
        PolicySnapshot snapshot,
        string transactionType,
        DateTime transactionTimestampUtc,
        decimal brokerPremium,
        DateOnly inceptionDate,
        DateOnly expiryDate,
        List<SectionOperation> sectionOperations)
    {
        return new CanonicalRiskRequest
        {
            EntityId = Guid.NewGuid(),
            ExternalReference = snapshot.PolicyReference,
            ProductCode = snapshot.ProductCode,
            SourceSystem = InternalSourceCode,
            TransactionType = transactionType,
            SchemeCode = "STANDARD",
            TransactionTimestampUtc = transactionTimestampUtc,
            LifecycleStatus = transactionType,
            AnnualizedGrossPremium = brokerPremium,
            CurrencyCode = string.IsNullOrWhiteSpace(snapshot.CurrencyCode) ? "USD" : snapshot.CurrencyCode,
            UnderwriterName = "Internal Lifecycle",
            PaymentMethod = "Invoice",
            Submission = new SubmissionData
            {
                UnderwritingYear = snapshot.UnderwritingYear,
                ChannelCode = "Internal",
                BrokerPremium = brokerPremium,
                TechnicalPremium = brokerPremium,
                IsRenewal = false
            },
            Broker = new BrokerData
            {
                BrokerCode = snapshot.Broker.Code,
                BrokerName = snapshot.Broker.Name,
                HasDelegatedAuthority = false,
                IsPreferredPartner = false
            },
            Insured = new InsuredData
            {
                FullName = snapshot.Insured.Name,
                TradingName = snapshot.Insured.TradingName,
                SegmentCode = "SME",
                EmployeeCount = 0,
                YearsInBusiness = 0
            },
            Quote = new QuoteData
            {
                QuoteReference = snapshot.QuoteReference,
                EffectiveDate = inceptionDate,
                ExpiryDate = expiryDate,
                QuoteStatusHint = snapshot.Lifecycle.QuoteStatus
            },
            Policy = new PolicyData
            {
                PolicyReference = snapshot.PolicyReference,
                InceptionDate = inceptionDate,
                ExpiryDate = expiryDate
            },
            Clearance = new ClearanceData
            {
                AutoClearanceEnabled = true,
                PremiumThreshold = decimal.MaxValue,
                FuzzyMatchTolerance = int.MaxValue
            },
            Enrichments = [],
            ContractChecks =
            [
                new ContractCheck { Code = "CONTRACT_LIFECYCLE", Description = $"{transactionType} requested via internal endpoint", IsComplete = true }
            ],
            ComplianceChecks =
            [
                new ComplianceCheck { Code = "COMPLIANCE_LIFECYCLE", Description = $"{transactionType} compliance baseline", IsComplete = true }
            ],
            Parties =
            [
                new PartyData { Role = "Insured", Name = snapshot.Insured.Name ?? string.Empty }
            ],
            Claims = [],
            Sections = [],
            SectionOperations = sectionOperations,
            Installments = []
        };
    }
}
