using System.Text.Json;
using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using InsuranceIntegration.Api.SourceContracts.Risks;

namespace InsuranceIntegration.Api.Mappers.Risks;

public sealed class BindPointRiskMapper(TimeProvider timeProvider) : ISourceRiskMapper
{
    private const string SystemCode = "BINDPOINT";
    private const string SupportedMessageType = "PolicyBindRequest";

    public bool CanMap(SourceIngestRequest request)
    {
        return string.Equals(request.SourceSystem, SystemCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.MessageType, SupportedMessageType, StringComparison.OrdinalIgnoreCase);
    }

    public CanonicalRiskRequest Map(SourceIngestRequest request)
    {
        var payload = request.Payload.Deserialize<BindPointPolicyBindPayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Unable to deserialize BindPoint policy bind payload.");

        var transactionTimestampUtc = timeProvider.GetUtcNow().UtcDateTime;
        var installmentAmount = payload.InstallmentCount > 0
            ? Math.Round(payload.BoundPremium / payload.InstallmentCount, 2, MidpointRounding.AwayFromZero)
            : 0m;

        // Rounded equal installments may not sum exactly to the bound premium
        // (e.g. 1000 / 3 = 333.33 × 3 = 999.99). Put the residual on the last
        // installment so the schedule always reconciles to the premium.
        var roundingResidual = payload.InstallmentCount > 0
            ? payload.BoundPremium - (installmentAmount * payload.InstallmentCount)
            : 0m;

        var installments = Enumerable.Range(1, Math.Max(payload.InstallmentCount, 0))
            .Select(index => new InstallmentData
            {
                SequenceNumber = index,
                DueDate = payload.InceptionDate.AddMonths(index - 1),
                Amount = index == payload.InstallmentCount
                    ? installmentAmount + roundingResidual
                    : installmentAmount,
                IsPaid = false
            })
            .ToList();

        return new CanonicalRiskRequest
        {
            EntityId = Guid.NewGuid(),
            ExternalReference = payload.PolicyReference,
            ProductCode = payload.ProductCode.ToUpperInvariant(),
            SourceSystem = SystemCode,
            TransactionType = "PolicyBind",
            SchemeCode = "STANDARD",
            TransactionTimestampUtc = transactionTimestampUtc,
            BoundDate = payload.BoundDate ?? DateOnly.FromDateTime(transactionTimestampUtc.Date),
            LifecycleStatus = "ReadyToBind",
            AnnualizedGrossPremium = payload.BoundPremium,
            CurrencyCode = payload.CurrencyCode,
            UnderwriterName = "BindPoint Intake",
            PaymentMethod = payload.PaymentMethod ?? "Invoice",
            Submission = new SubmissionData
            {
                UnderwritingYear = payload.InceptionDate.Year,
                ChannelCode = "Broker",
                BrokerPremium = payload.BoundPremium,
                TechnicalPremium = payload.BoundPremium,
                IsRenewal = false
            },
            Broker = new BrokerData
            {
                BrokerCode = payload.BrokerCode,
                BrokerName = payload.BrokerName,
                HasDelegatedAuthority = payload.BrokerHasDelegatedAuthority,
                IsPreferredPartner = payload.BrokerIsPreferredPartner
            },
            Insured = new InsuredData
            {
                FullName = payload.InsuredName,
                TradingName = payload.InsuredName,
                SegmentCode = "SME",
                EmployeeCount = 0,
                YearsInBusiness = 0
            },
            Quote = new QuoteData
            {
                QuoteReference = payload.QuoteReference,
                EffectiveDate = payload.InceptionDate,
                ExpiryDate = payload.ExpiryDate,
                QuoteStatusHint = "Quoted"
            },
            Policy = new PolicyData
            {
                PolicyReference = payload.PolicyReference,
                InceptionDate = payload.InceptionDate,
                ExpiryDate = payload.ExpiryDate
            },
            Clearance = new ClearanceData
            {
                AutoClearanceEnabled = true,
                PremiumThreshold = 100000m,
                FuzzyMatchTolerance = 2
            },
            Enrichments = [],
            ContractChecks =
            [
                new() { Code = "CONTRACT_SIGNALS", IsComplete = true, Description = "Bound contract indicators present" }
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
            Sections = [],
            SectionOperations = [],
            Installments = installments
        };
    }
}
