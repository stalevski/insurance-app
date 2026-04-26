namespace InsuranceIntegration.Api.Responses.Risks;

public sealed class FinalRiskResponse
{
    public Guid EntityId { get; init; }

    public string ExternalReference { get; init; } = string.Empty;

    public string ProductCode { get; init; } = string.Empty;

    public string SourceSystem { get; init; } = string.Empty;

    public string TransactionType { get; init; } = string.Empty;

    public string SubmissionStatus { get; init; } = string.Empty;

    public string QuoteStatus { get; init; } = string.Empty;

    public string PolicyStatus { get; init; } = string.Empty;

    public string BrokerDecision { get; init; } = string.Empty;

    public string InsuredDecision { get; init; } = string.Empty;

    public int ClaimCount { get; init; }

    public int SectionCount { get; init; }

    public int InstallmentCount { get; init; }

    public int SectionOperationCount { get; init; }

    public int SubcoverOperationCount { get; init; }

    public int BlockingEnrichmentCount { get; init; }

    public string ClearanceDecision { get; init; } = string.Empty;

    public bool AutoCleared { get; init; }

    public decimal TotalIncurredAmount { get; init; }

    public decimal TotalReservedAmount { get; init; }

    public decimal BasePremium { get; init; }

    public decimal AdjustedPremium { get; init; }

    public int BestFuzzyMatchDistance { get; init; }

    public string BestFuzzyMatchDescription { get; init; } = string.Empty;

    public List<string> DecisionReasons { get; init; } = [];

    public List<string> AppliedEnrichments { get; init; } = [];

    public List<string> SectionActions { get; init; } = [];

    public decimal TotalSumInsured { get; init; }

    public decimal TotalSectionPremium { get; init; }

    public bool PremiumAllocationBalanced { get; init; }

    public List<string> CoverageWarnings { get; init; } = [];

    public string FinalStatus { get; init; } = string.Empty;
}
