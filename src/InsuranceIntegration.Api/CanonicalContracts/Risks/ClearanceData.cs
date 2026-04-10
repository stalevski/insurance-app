namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class ClearanceData
{
    public bool AutoClearanceEnabled { get; init; }

    public decimal PremiumThreshold { get; init; }

    public int FuzzyMatchTolerance { get; init; }
}
