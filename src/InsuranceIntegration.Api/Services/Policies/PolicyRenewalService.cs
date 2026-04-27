using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Events;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.Snapshots.Policies;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Services.Policies;

public sealed class PolicyRenewalService : IPolicyRenewalService
{
    private const string InternalSourceCode = "internal";

    private readonly IPolicySnapshotService _policySnapshotService;
    private readonly IRiskFlowService _riskFlowService;
    private readonly IRiskSnapshotRouter _riskSnapshotRouter;
    private readonly IntegrationDbContext _context;
    private readonly TimeProvider _time;

    public PolicyRenewalService(
        IPolicySnapshotService policySnapshotService,
        IRiskFlowService riskFlowService,
        IRiskSnapshotRouter riskSnapshotRouter,
        IntegrationDbContext context,
        TimeProvider time)
    {
        _policySnapshotService = policySnapshotService;
        _riskFlowService = riskFlowService;
        _riskSnapshotRouter = riskSnapshotRouter;
        _context = context;
        _time = time;
    }

    public RenewalResult ApplyRenewal(RenewalRequest request)
    {
        var prior = _policySnapshotService.Find(request.PolicyReference)
            ?? throw new KeyNotFoundException($"Policy '{request.PolicyReference}' was not found. Cannot renew.");

        ValidatePriorPolicyIsRenewable(prior);
        ValidateDates(request);

        var nowUtc = _time.GetUtcNow().UtcDateTime;

        // 1) Pricing
        var lossRatio = request.PriorAnnualPremium > 0m
            ? Math.Round(request.PriorClaimsPaid / request.PriorAnnualPremium, 4, MidpointRounding.AwayFromZero)
            : 0m;
        var (band, lossRatioLoad) = ResolveLossRatioBand(lossRatio);
        var exposureLoad = Math.Round(request.RevenueDeltaPercent / 2m, 4, MidpointRounding.AwayFromZero);
        var overrideLoad = request.OverrideLoadPercent ?? 0m;
        var totalLoad = lossRatioLoad + exposureLoad + overrideLoad;
        var renewalPremium = Math.Round(request.PriorAnnualPremium * (1m + totalLoad), 2, MidpointRounding.AwayFromZero);
        if (renewalPremium < 0m)
        {
            renewalPremium = 0m;
        }

        var reasons = new List<string>
        {
            $"Loss ratio: {lossRatio:0.####} ({band})",
            $"Loss-ratio load: {lossRatioLoad:+0.##%;-0.##%;0.##%}",
            $"Exposure load (50% of revenue delta {request.RevenueDeltaPercent:+0.##%;-0.##%;0.##%}): {exposureLoad:+0.##%;-0.##%;0.##%}",
            $"Override load: {overrideLoad:+0.##%;-0.##%;0.##%}",
            $"Total load applied to prior premium {request.PriorAnnualPremium:0.##}: {totalLoad:+0.##%;-0.##%;0.##%}",
            $"Renewal premium: {renewalPremium:0.##}"
        };

        // 2) Mark prior policy as Renewed (policy aggregate event)
        var priorRenewedRequest = SynthesizePriorPolicyRenewed(prior, request, nowUtc);
        var priorContext = BuildInternalContext("PolicyRenewed", request.PolicyReference, nowUtc);
        var priorResponse = _riskFlowService.Process(priorRenewedRequest);
        _riskSnapshotRouter.Route(priorRenewedRequest, priorResponse, priorContext);
        var policyRenewedEventId = LookupEventId(DomainEventAggregateKind.Policy, request.PolicyReference, priorContext.EnvelopeId);

        // 3) Issue the renewal quote (quote aggregate event)
        var renewalQuoteRequest = SynthesizeRenewalQuote(prior, request, renewalPremium, nowUtc);
        var quoteContext = BuildInternalContext("QuoteIssued", request.NewQuoteReference, nowUtc);
        var quoteResponse = _riskFlowService.Process(renewalQuoteRequest);
        _riskSnapshotRouter.Route(renewalQuoteRequest, quoteResponse, quoteContext);
        var quoteIssuedEventId = LookupEventId(DomainEventAggregateKind.Quote, request.NewQuoteReference, quoteContext.EnvelopeId);

        return new RenewalResult
        {
            PriorPolicyReference = request.PolicyReference,
            NewQuoteReference = request.NewQuoteReference,
            PriorAnnualPremium = request.PriorAnnualPremium,
            LossRatio = lossRatio,
            LossRatioBand = band,
            LossRatioLoadPercent = lossRatioLoad,
            ExposureLoadPercent = exposureLoad,
            OverrideLoadPercent = overrideLoad,
            RenewalPremium = renewalPremium,
            PolicyRenewedEventId = policyRenewedEventId,
            QuoteIssuedEventId = quoteIssuedEventId,
            Reasons = reasons
        };
    }

    private static void ValidatePriorPolicyIsRenewable(PolicySnapshot prior)
    {
        var phase = prior.Lifecycle.CurrentPhase;
        if (string.Equals(phase, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Policy '{prior.PolicyReference}' is cancelled and cannot be renewed.");
        }

        if (string.Equals(phase, "Renewed", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Policy '{prior.PolicyReference}' has already been renewed.");
        }
    }

    private static void ValidateDates(RenewalRequest request)
    {
        if (request.NewExpiryDate <= request.NewInceptionDate)
        {
            throw new ArgumentException("Renewal expiry must be after inception.");
        }
    }

    private static (string Band, decimal Load) ResolveLossRatioBand(decimal lossRatio)
    {
        return lossRatio switch
        {
            < 0.30m => ("Excellent", -0.05m),
            <= 0.60m => ("Standard", 0m),
            <= 0.80m => ("Loaded", 0.10m),
            <= 1.00m => ("HeavilyLoaded", 0.25m),
            _ => ("Distressed", 0.40m)
        };
    }

    private static IngestContext BuildInternalContext(string messageType, string aggregateKey, DateTime occurredAtUtc)
    {
        return new IngestContext
        {
            Source = InternalSourceCode,
            EnvelopeId = $"{messageType.ToLowerInvariant()}-{aggregateKey}-{occurredAtUtc:yyyyMMddHHmmssfff}",
            MessageType = messageType,
            ReceivedAtUtc = occurredAtUtc,
            CorrelationId = null
        };
    }

    private Guid LookupEventId(string aggregateKind, string aggregateKey, string envelopeId)
    {
        return _context.DomainEvents
            .AsNoTracking()
            .Where(record => record.AggregateKind == aggregateKind
                && record.AggregateKey == aggregateKey
                && record.Source == InternalSourceCode
                && record.EnvelopeId == envelopeId)
            .OrderByDescending(record => record.RecordedAtUtc)
            .Select(record => record.Id)
            .FirstOrDefault();
    }

    private static CanonicalRiskRequest SynthesizePriorPolicyRenewed(PolicySnapshot prior, RenewalRequest request, DateTime nowUtc)
    {
        var inception = prior.Dates.InceptionDate ?? DateOnly.FromDateTime(nowUtc);
        var expiry = prior.Dates.ExpiryDate ?? DateOnly.FromDateTime(nowUtc.AddYears(1));

        return new CanonicalRiskRequest
        {
            EntityId = Guid.NewGuid(),
            ExternalReference = prior.PolicyReference,
            ProductCode = prior.ProductCode,
            SourceSystem = InternalSourceCode,
            TransactionType = PolicyTransactionType.Renewal,
            SchemeCode = "STANDARD",
            TransactionTimestampUtc = nowUtc,
            LifecycleStatus = "Renewal",
            AnnualizedGrossPremium = request.PriorAnnualPremium,
            CurrencyCode = string.IsNullOrWhiteSpace(prior.CurrencyCode) ? "USD" : prior.CurrencyCode,
            UnderwriterName = "Internal Renewal",
            PaymentMethod = "Invoice",
            Submission = new SubmissionData
            {
                UnderwritingYear = prior.UnderwritingYear,
                ChannelCode = "Internal",
                BrokerPremium = request.PriorAnnualPremium,
                TechnicalPremium = request.PriorAnnualPremium,
                IsRenewal = false
            },
            Broker = BuildBroker(prior),
            Insured = BuildInsured(prior),
            Quote = new QuoteData
            {
                QuoteReference = prior.QuoteReference,
                EffectiveDate = inception,
                ExpiryDate = expiry,
                QuoteStatusHint = "Renewed"
            },
            Policy = new PolicyData
            {
                PolicyReference = prior.PolicyReference,
                InceptionDate = inception,
                ExpiryDate = expiry
            },
            Clearance = BuildPermissiveClearance(),
            Enrichments = [],
            ContractChecks =
            [
                new ContractCheck { Code = "CONTRACT_RENEWAL", Description = "Renewal contract review", IsComplete = true }
            ],
            ComplianceChecks =
            [
                new ComplianceCheck { Code = "COMPLIANCE_RENEWAL", Description = "Renewal compliance baseline", IsComplete = true }
            ],
            Parties =
            [
                new PartyData { Role = "Insured", Name = prior.Insured.Name ?? string.Empty }
            ],
            Claims = [],
            Sections = [],
            SectionOperations = [],
            Installments = []
        };
    }

    private static CanonicalRiskRequest SynthesizeRenewalQuote(PolicySnapshot prior, RenewalRequest request, decimal renewalPremium, DateTime nowUtc)
    {
        return new CanonicalRiskRequest
        {
            EntityId = Guid.NewGuid(),
            ExternalReference = request.NewQuoteReference,
            ProductCode = prior.ProductCode,
            SourceSystem = InternalSourceCode,
            TransactionType = QuoteTransactionType.Quoted,
            SchemeCode = "STANDARD",
            TransactionTimestampUtc = nowUtc,
            LifecycleStatus = "Quoting",
            AnnualizedGrossPremium = renewalPremium,
            CurrencyCode = string.IsNullOrWhiteSpace(prior.CurrencyCode) ? "USD" : prior.CurrencyCode,
            UnderwriterName = "Internal Renewal",
            PaymentMethod = "Invoice",
            Submission = new SubmissionData
            {
                UnderwritingYear = request.NewInceptionDate.Year,
                ChannelCode = "Internal",
                BrokerPremium = renewalPremium,
                TechnicalPremium = renewalPremium,
                IsRenewal = true,
                PriorPolicyReference = prior.PolicyReference
            },
            Broker = BuildBroker(prior),
            Insured = BuildInsured(prior),
            Quote = new QuoteData
            {
                QuoteReference = request.NewQuoteReference,
                EffectiveDate = request.NewInceptionDate,
                ExpiryDate = request.NewExpiryDate,
                QuoteStatusHint = "Quoted"
            },
            Policy = new PolicyData(),
            Clearance = BuildPermissiveClearance(),
            Enrichments = [],
            ContractChecks =
            [
                new ContractCheck { Code = "CONTRACT_RENEWAL_QUOTE", Description = "Renewal quote contract review", IsComplete = true }
            ],
            ComplianceChecks =
            [
                new ComplianceCheck { Code = "COMPLIANCE_RENEWAL_QUOTE", Description = "Renewal quote compliance baseline", IsComplete = true }
            ],
            Parties =
            [
                new PartyData { Role = "Insured", Name = prior.Insured.Name ?? string.Empty }
            ],
            Claims = [],
            Sections = [],
            SectionOperations = [],
            Installments = []
        };
    }

    private static BrokerData BuildBroker(PolicySnapshot prior)
    {
        return new BrokerData
        {
            BrokerCode = prior.Broker.Code,
            BrokerName = prior.Broker.Name,
            HasDelegatedAuthority = false,
            IsPreferredPartner = false
        };
    }

    private static InsuredData BuildInsured(PolicySnapshot prior)
    {
        return new InsuredData
        {
            FullName = prior.Insured.Name,
            TradingName = prior.Insured.TradingName,
            SegmentCode = "SME",
            EmployeeCount = 0,
            YearsInBusiness = 0
        };
    }

    private static ClearanceData BuildPermissiveClearance()
    {
        return new ClearanceData
        {
            AutoClearanceEnabled = true,
            PremiumThreshold = decimal.MaxValue,
            FuzzyMatchTolerance = int.MaxValue
        };
    }
}
