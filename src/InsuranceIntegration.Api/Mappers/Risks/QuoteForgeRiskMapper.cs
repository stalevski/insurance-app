using System.Text.Json;
using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using InsuranceIntegration.Api.SourceContracts.Risks;

namespace InsuranceIntegration.Api.Mappers.Risks;

public sealed class QuoteForgeRiskMapper(TimeProvider timeProvider) : ISourceRiskMapper
{
    private const string SystemCode = "QUOTEFORGE";
    private const string SupportedMessageType = "QuoteRequest";

    public bool CanMap(SourceIngestRequest request)
    {
        return string.Equals(request.SourceSystem, SystemCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.MessageType, SupportedMessageType, StringComparison.OrdinalIgnoreCase);
    }

    public CanonicalRiskRequest Map(SourceIngestRequest request)
    {
        var payload = request.Payload.Deserialize<QuoteForgeQuoteRequestPayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Unable to deserialize QuoteForge quote request payload.");

        var transactionTimestampUtc = timeProvider.GetUtcNow().UtcDateTime;

        return new CanonicalRiskRequest
        {
            EntityId = Guid.NewGuid(),
            ExternalReference = payload.QuoteReference,
            ProductCode = payload.ProductLine.ToUpperInvariant(),
            SourceSystem = SystemCode,
            TransactionType = "Quote",
            SchemeCode = "STANDARD",
            TransactionTimestampUtc = transactionTimestampUtc,
            LifecycleStatus = "Quoting",
            AnnualizedGrossPremium = payload.BrokerPremium ?? payload.TechnicalPremium,
            CurrencyCode = payload.CurrencyCode,
            UnderwriterName = "QuoteForge Intake",
            PaymentMethod = "Invoice",
            Submission = new SubmissionData
            {
                UnderwritingYear = payload.UnderwritingYear == 0 ? transactionTimestampUtc.Year : payload.UnderwritingYear,
                ChannelCode = "Broker",
                BrokerPremium = payload.BrokerPremium,
                TechnicalPremium = payload.TechnicalPremium,
                Revenue = payload.InsuredRevenue,
                IsRenewal = false
            },
            Broker = new BrokerData
            {
                BrokerCode = payload.BrokerCode,
                BrokerName = payload.BrokerName,
                HasDelegatedAuthority = false,
                IsPreferredPartner = !string.IsNullOrWhiteSpace(payload.BrokerCode)
            },
            Insured = new InsuredData
            {
                FullName = payload.InsuredName,
                TradingName = payload.InsuredName,
                SegmentCode = "SME",
                AnnualRevenue = payload.InsuredRevenue,
                EmployeeCount = payload.InsuredEmployeeCount,
                YearsInBusiness = payload.InsuredYearsInBusiness
            },
            Quote = new QuoteData
            {
                QuoteReference = payload.QuoteReference,
                EffectiveDate = payload.EffectiveDate ?? DateOnly.FromDateTime(transactionTimestampUtc.Date.AddDays(7)),
                ExpiryDate = payload.ExpiryDate ?? DateOnly.FromDateTime(transactionTimestampUtc.Date.AddYears(1).AddDays(6)),
                QuoteStatusHint = "Indicative"
            },
            Policy = new PolicyData(),
            Clearance = new ClearanceData
            {
                AutoClearanceEnabled = true,
                PremiumThreshold = 50000m,
                FuzzyMatchTolerance = 3
            },
            Enrichments =
            [
                new() { Family = "Universal", Code = "TRIAGE_STANDARD", Description = "Universal triage baseline", Multiplier = 1.02m, IsBlocking = false, IsDerived = false }
            ],
            ContractChecks =
            [
                new() { Code = "CONTRACT_SIGNALS", IsComplete = true, Description = "Core contract indicators present" }
            ],
            ComplianceChecks =
            [
                new() { Code = "KYC_BASELINE", IsComplete = true, Description = "Baseline compliance record complete" }
            ],
            Parties =
            [
                new() { Role = "Insured", Name = payload.InsuredName },
                new() { Role = "Broker", Name = payload.BrokerName ?? "Unknown Broker" }
            ],
            Claims = [],
            Sections =
            [
                new() { SectionCode = "CORE", SectionName = payload.ProductLine, Subcovers = [] }
            ],
            SectionOperations =
            [
                new() { SectionCode = "CORE", OperationType = "AddSection" }
            ],
            Installments = []
        };
    }
}
