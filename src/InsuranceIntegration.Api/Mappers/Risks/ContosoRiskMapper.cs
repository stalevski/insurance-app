using System.Text.Json;
using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using InsuranceIntegration.Api.SourceContracts.Risks;

namespace InsuranceIntegration.Api.Mappers.Risks;

public sealed class ContosoRiskMapper(TimeProvider timeProvider) : ISourceRiskMapper
{
    public bool CanMap(SourceIngestRequest request)
    {
        return string.Equals(request.SourceSystem, "CONTOSO_UW", StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.MessageType, "RiskSubmission", StringComparison.OrdinalIgnoreCase);
    }

    public CanonicalRiskRequest Map(SourceIngestRequest request)
    {
        var payload = request.Payload.Deserialize<ContosoRiskSubmissionPayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (payload is null)
        {
            throw new InvalidOperationException("Unable to deserialize Contoso risk payload.");
        }

        var transactionTimestampUtc = timeProvider.GetUtcNow().UtcDateTime;
        var entityId = Guid.NewGuid();

        return new CanonicalRiskRequest
        {
            EntityId = entityId,
            ExternalReference = payload.QuoteId,
            ProductCode = payload.Trade.ToUpperInvariant(),
            SourceSystem = "CONTOSO_UW",
            TransactionType = "Submission",
            SchemeCode = "STANDARD",
            TransactionTimestampUtc = transactionTimestampUtc,
            LifecycleStatus = "Ingested",
            AnnualizedGrossPremium = payload.EstimatedPremium,
            CurrencyCode = "USD",
            UnderwriterName = "Contoso Intake",
            PaymentMethod = "Invoice",
            Submission = new SubmissionData
            {
                UnderwritingYear = transactionTimestampUtc.Year,
                ChannelCode = "DigitalBroker",
                TechnicalPremium = payload.EstimatedPremium,
                Revenue = 1250000m,
                IsRenewal = false
            },
            Broker = new BrokerData
            {
                BrokerCode = "BRK-CONTOSO-01",
                BrokerName = "Summit Risk Partners",
                HasDelegatedAuthority = false,
                IsPreferredPartner = true
            },
            Insured = new InsuredData
            {
                FullName = payload.InsuredName,
                TradingName = payload.InsuredName,
                SegmentCode = "SME",
                AnnualRevenue = 1250000m,
                EmployeeCount = 24,
                YearsInBusiness = 11
            },
            Quote = new QuoteData
            {
                QuoteReference = payload.QuoteId,
                EffectiveDate = DateOnly.FromDateTime(transactionTimestampUtc.Date.AddDays(7)),
                ExpiryDate = DateOnly.FromDateTime(transactionTimestampUtc.Date.AddYears(1).AddDays(6)),
                QuoteStatusHint = "Indicative"
            },
            Policy = new PolicyData(),
            Clearance = new ClearanceData
            {
                AutoClearanceEnabled = true,
                PremiumThreshold = 25000m,
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
                new() { Role = "Broker", Name = "Summit Risk Partners" }
            ],
            Claims =
            [
                new() { ClaimReference = "CLM-100", ClaimantName = payload.InsuredName, IncurredAmount = 1250m, ReservedAmount = 200m }
            ],
            Sections =
            [
                new() { SectionCode = "CORE", SectionName = payload.Trade, Subcovers = [ new() { SubcoverCode = "BASE", SubcoverName = "Base Cover" } ] },
                new() { SectionCode = "LIAB", SectionName = "Liability", Subcovers = [] }
            ],
            SectionOperations =
            [
                new() { SectionCode = "CORE", OperationType = "AddSection" },
                new() { SectionCode = "CORE", OperationType = "AddSubcover", SubcoverCode = "BASE" },
                new() { SectionCode = "LIAB", OperationType = "RemoveAllSubcovers", RemoveAllSubcovers = true }
            ],
            Installments =
            [
                new() { SequenceNumber = 1, DueDate = DateOnly.FromDateTime(transactionTimestampUtc.Date.AddDays(30)), Amount = Math.Round(payload.EstimatedPremium / 2m, 2) },
                new() { SequenceNumber = 2, DueDate = DateOnly.FromDateTime(transactionTimestampUtc.Date.AddDays(60)), Amount = Math.Round(payload.EstimatedPremium / 2m, 2) }
            ]
        };
    }
}
