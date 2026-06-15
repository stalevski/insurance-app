using InsuranceIntegration.Api.CanonicalContracts.Risks;

namespace InsuranceIntegration.Api.IntegrationTests.Builders;

/// <summary>
/// Fluent builder for a fully-populated <see cref="CanonicalRiskRequest"/> — the canonical payload
/// accepted by <c>POST /api/v1/risks</c>. Defaults describe a clean, bindable commercial-property
/// submission; <c>With…</c> methods tweak only the facets a given test cares about.
/// </summary>
public sealed class CanonicalRiskRequestBuilder
{
    private Guid _entityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private string _externalReference = "EXT-1001";
    private string _productCode = "COMMERCIAL_PROPERTY";
    private string _sourceSystem = "INTEGRATION_TESTS";
    private string _transactionType = "Submission";
    private string _insuredName = "Northwind Storage Ltd";
    private string _currencyCode = "USD";
    private int _underwritingYear = 2026;
    private decimal _insuredRevenue = 500_000m;
    private decimal _brokerPremium = 1_000m;
    private decimal _technicalPremium = 900m;
    private decimal _annualizedPremium = 800m;
    private bool _preferredBroker = true;
    private bool _autoClearanceEnabled = true;
    private bool _checksComplete = true;
    private int _claimCount = 1;
    private decimal _incurredPerClaim = 500m;
    private decimal _reservedPerClaim = 100m;
    private string _quoteReference = "Q-1001";
    private string _policyReference = string.Empty;

    public CanonicalRiskRequestBuilder WithProductCode(string productCode)
    {
        _productCode = productCode;
        return this;
    }

    public CanonicalRiskRequestBuilder WithTransactionType(string transactionType)
    {
        _transactionType = transactionType;
        return this;
    }

    public CanonicalRiskRequestBuilder WithExternalReference(string externalReference)
    {
        _externalReference = externalReference;
        return this;
    }

    public CanonicalRiskRequestBuilder WithSourceSystem(string sourceSystem)
    {
        _sourceSystem = sourceSystem;
        return this;
    }

    public CanonicalRiskRequestBuilder WithInsuredName(string insuredName)
    {
        _insuredName = insuredName;
        return this;
    }

    public CanonicalRiskRequestBuilder WithInsuredRevenue(decimal insuredRevenue)
    {
        _insuredRevenue = insuredRevenue;
        return this;
    }

    public CanonicalRiskRequestBuilder WithUnderwritingYear(int underwritingYear)
    {
        _underwritingYear = underwritingYear;
        return this;
    }

    public CanonicalRiskRequestBuilder WithPremiums(decimal brokerPremium, decimal technicalPremium, decimal annualizedPremium)
    {
        _brokerPremium = brokerPremium;
        _technicalPremium = technicalPremium;
        _annualizedPremium = annualizedPremium;
        return this;
    }

    public CanonicalRiskRequestBuilder WithQuoteReference(string quoteReference)
    {
        _quoteReference = quoteReference;
        return this;
    }

    public CanonicalRiskRequestBuilder WithPolicyReference(string policyReference)
    {
        _policyReference = policyReference;
        return this;
    }

    public CanonicalRiskRequestBuilder WithClaims(int claimCount, decimal incurredPerClaim = 500m, decimal reservedPerClaim = 100m)
    {
        _claimCount = claimCount;
        _incurredPerClaim = incurredPerClaim;
        _reservedPerClaim = reservedPerClaim;
        return this;
    }

    public CanonicalRiskRequestBuilder WithPreferredBroker(bool preferredBroker)
    {
        _preferredBroker = preferredBroker;
        return this;
    }

    public CanonicalRiskRequestBuilder WithCompletedChecks(bool checksComplete)
    {
        _checksComplete = checksComplete;
        return this;
    }

    public CanonicalRiskRequest Build()
    {
        var claims = Enumerable.Range(1, _claimCount)
            .Select(index => new ClaimData
            {
                ClaimReference = $"CLM-{index}",
                ClaimantName = _insuredName,
                IncurredAmount = _incurredPerClaim,
                ReservedAmount = _reservedPerClaim,
            })
            .ToList();

        return new CanonicalRiskRequest
        {
            EntityId = _entityId,
            ExternalReference = _externalReference,
            ProductCode = _productCode,
            SourceSystem = _sourceSystem,
            TransactionType = _transactionType,
            SchemeCode = "STANDARD",
            TransactionTimestampUtc = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc),
            LifecycleStatus = "Ingested",
            AnnualizedGrossPremium = _annualizedPremium,
            CurrencyCode = _currencyCode,
            UnderwriterName = "Integration Tests",
            PaymentMethod = "Invoice",
            Submission = new SubmissionData
            {
                UnderwritingYear = _underwritingYear,
                BrokerPremium = _brokerPremium,
                TechnicalPremium = _technicalPremium,
                Revenue = _insuredRevenue,
                IsRenewal = false,
            },
            Broker = new BrokerData
            {
                BrokerCode = "BRK-1",
                BrokerName = "Summit Risk Partners",
                HasDelegatedAuthority = false,
                IsPreferredPartner = _preferredBroker,
            },
            Insured = new InsuredData
            {
                FullName = _insuredName,
                TradingName = _insuredName,
                SegmentCode = "SME",
                AnnualRevenue = _insuredRevenue,
                EmployeeCount = 20,
                YearsInBusiness = 5,
            },
            Quote = new QuoteData
            {
                QuoteReference = _quoteReference,
                EffectiveDate = new DateOnly(_underwritingYear, 5, 1),
                ExpiryDate = new DateOnly(_underwritingYear + 1, 4, 30),
                QuoteStatusHint = "Indicative",
            },
            Policy = new PolicyData
            {
                PolicyReference = _policyReference,
                InceptionDate = new DateOnly(_underwritingYear, 1, 1),
                ExpiryDate = new DateOnly(_underwritingYear, 12, 31),
            },
            Clearance = new ClearanceData
            {
                AutoClearanceEnabled = _autoClearanceEnabled,
                PremiumThreshold = 5_000m,
                FuzzyMatchTolerance = 3,
                AutoClearIncurredThreshold = 5_000m,
                DeclineIncurredThreshold = 25_000m,
            },
            Enrichments =
            [
                new EnrichmentItem
                {
                    Family = "Universal",
                    Code = "TRIAGE_STANDARD",
                    Description = "Universal triage baseline",
                    Multiplier = 1.02m,
                    IsBlocking = false,
                    IsDerived = false,
                },
            ],
            ContractChecks =
            [
                new ContractCheck { Code = "CONTRACT", IsComplete = _checksComplete, Description = "Contract check" },
            ],
            ComplianceChecks =
            [
                new ComplianceCheck { Code = "COMPLIANCE", IsComplete = _checksComplete, Description = "Compliance check" },
            ],
            Parties =
            [
                new PartyData { Role = "Insured", Name = _insuredName },
            ],
            Claims = claims,
            Sections =
            [
                new SectionData
                {
                    SectionCode = "PROP",
                    SectionName = "Property",
                    Subcovers = [new SubcoverData { SubcoverCode = "SUB-1", SubcoverName = "Subcover 1" }],
                },
            ],
            SectionOperations =
            [
                new SectionOperation { SectionCode = "PROP", OperationType = "AddSection" },
                new SectionOperation { SectionCode = "PROP", OperationType = "AddSubcover", SubcoverCode = "SUB-1" },
            ],
            Installments =
            [
                new InstallmentData { SequenceNumber = 1, DueDate = new DateOnly(2026, 6, 1), Amount = 500m, IsPaid = false },
            ],
        };
    }
}
