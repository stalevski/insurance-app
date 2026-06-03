namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

/// <summary>
/// Property-specific risk attributes. All members are optional; absent values
/// simply mean the corresponding signal is not evaluated.
/// </summary>
public sealed class PropertyRiskDetails
{
    public string? ConstructionType { get; init; }

    public int? YearBuilt { get; init; }

    public bool? Sprinklered { get; init; }

    public string? FloodZone { get; init; }

    public decimal? BuildingValue { get; init; }
}
