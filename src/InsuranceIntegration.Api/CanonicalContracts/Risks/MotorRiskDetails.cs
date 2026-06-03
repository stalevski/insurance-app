namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

/// <summary>
/// Motor-specific risk attributes. All members are optional; absent values
/// simply mean the corresponding signal is not evaluated.
/// </summary>
public sealed class MotorRiskDetails
{
    public int? FleetSize { get; init; }

    public string? PrimaryUse { get; init; }

    public int? YoungestDriverAge { get; init; }

    public bool? CommercialUse { get; init; }
}
