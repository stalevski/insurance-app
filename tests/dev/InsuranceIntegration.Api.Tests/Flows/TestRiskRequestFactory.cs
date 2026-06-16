using InsuranceIntegration.Api.CanonicalContracts.Risks;

namespace InsuranceIntegration.Api.Tests.Flows;

internal static class TestRiskRequestFactory
{
    public static CanonicalRiskRequest Create(
        decimal? brokerPremium = 1000m,
        decimal? technicalPremium = 900m,
        decimal? annualizedPremium = 800m,
        string productCode = "COMMERCIAL_PROPERTY",
        int underwritingYear = 2026,
        decimal insuredRevenue = 500000m,
        int yearsInBusiness = 5,
        bool preferredBroker = true,
        bool delegatedAuthority = false,
        bool autoClearanceEnabled = true,
        decimal premiumThreshold = 5000m,
        int fuzzyMatchTolerance = 3,
        decimal autoClearIncurredThreshold = 5000m,
        decimal declineIncurredThreshold = 25000m,
        int claimCount = 1,
        decimal incurredPerClaim = 500m,
        decimal reservedPerClaim = 100m,
        bool checksComplete = true,
        int subcoverCount = 1,
        string transactionType = "Submission",
        string insuredName = "Northwind Storage Ltd",
        string externalReference = "EXT-1001",
        string? brokerCode = "BRK-1",
        string? policyReference = null)
    {
        var claims = Enumerable.Range(1, claimCount)
            .Select(index => new ClaimData
            {
                ClaimReference = $"CLM-{index}",
                ClaimantName = insuredName,
                IncurredAmount = incurredPerClaim,
                ReservedAmount = reservedPerClaim
            })
            .ToList();

        var subcovers = Enumerable.Range(1, subcoverCount)
            .Select(index => new SubcoverData
            {
                SubcoverCode = $"SUB-{index}",
                SubcoverName = $"Subcover {index}"
            })
            .ToList();

        return new CanonicalRiskRequest
        {
            EntityId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ExternalReference = externalReference,
            ProductCode = productCode,
            SourceSystem = "UNIT_TEST",
            TransactionType = transactionType,
            SchemeCode = "STANDARD",
            TransactionTimestampUtc = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc),
            LifecycleStatus = "Ingested",
            AnnualizedGrossPremium = annualizedPremium,
            CurrencyCode = "USD",
            UnderwriterName = "Unit Test",
            PaymentMethod = "Invoice",
            Submission = new SubmissionData
            {
                UnderwritingYear = underwritingYear,
                BrokerPremium = brokerPremium,
                TechnicalPremium = technicalPremium,
                Revenue = insuredRevenue,
                IsRenewal = false
            },
            Broker = new BrokerData
            {
                BrokerCode = brokerCode,
                BrokerName = "Summit Risk Partners",
                HasDelegatedAuthority = delegatedAuthority,
                IsPreferredPartner = preferredBroker
            },
            Insured = new InsuredData
            {
                FullName = insuredName,
                TradingName = insuredName,
                SegmentCode = "SME",
                AnnualRevenue = insuredRevenue,
                EmployeeCount = 20,
                YearsInBusiness = yearsInBusiness
            },
            Quote = new QuoteData
            {
                QuoteReference = "Q-1001",
                EffectiveDate = new DateOnly(2026, 5, 1),
                ExpiryDate = new DateOnly(2027, 4, 30),
                QuoteStatusHint = "Indicative"
            },
            Policy = new PolicyData
            {
                PolicyReference = policyReference ?? string.Empty,
                InceptionDate = new DateOnly(underwritingYear, 1, 1),
                ExpiryDate = new DateOnly(underwritingYear, 12, 31)
            },
            Clearance = new ClearanceData
            {
                AutoClearanceEnabled = autoClearanceEnabled,
                PremiumThreshold = premiumThreshold,
                FuzzyMatchTolerance = fuzzyMatchTolerance,
                AutoClearIncurredThreshold = autoClearIncurredThreshold,
                DeclineIncurredThreshold = declineIncurredThreshold
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
                    IsDerived = false
                }
            ],
            ContractChecks =
            [
                new ContractCheck { Code = "CONTRACT", IsComplete = checksComplete, Description = "Contract check" }
            ],
            ComplianceChecks =
            [
                new ComplianceCheck { Code = "COMPLIANCE", IsComplete = checksComplete, Description = "Compliance check" }
            ],
            Parties =
            [
                new PartyData { Role = "Insured", Name = insuredName }
            ],
            Claims = claims,
            Sections =
            [
                new SectionData
                {
                    SectionCode = "PROP",
                    SectionName = "Property",
                    Subcovers = subcovers
                }
            ],
            SectionOperations =
            [
                new SectionOperation { SectionCode = "PROP", OperationType = "AddSection" },
                new SectionOperation { SectionCode = "PROP", OperationType = "AddSubcover", SubcoverCode = "SUB-1" }
            ],
            Installments =
            [
                new InstallmentData { SequenceNumber = 1, DueDate = new DateOnly(2026, 6, 1), Amount = 500m, IsPaid = false }
            ]
        };
    }
}
