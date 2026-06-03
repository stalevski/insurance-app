namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

/// <summary>
/// Liability-specific risk attributes. All members are optional; absent values
/// simply mean the corresponding signal is not evaluated.
/// </summary>
public sealed class LiabilityRiskDetails
{
    public string? Profession { get; init; }

    public int? ContractorCount { get; init; }

    public bool? HasPriorClaims { get; init; }
}
