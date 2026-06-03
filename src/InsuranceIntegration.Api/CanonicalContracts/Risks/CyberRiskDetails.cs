namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

/// <summary>
/// Cyber-specific risk attributes. All members are optional; absent values
/// simply mean the corresponding signal is not evaluated.
/// </summary>
public sealed class CyberRiskDetails
{
    public long? SensitiveRecordsHeld { get; init; }

    public bool? MultiFactorAuthentication { get; init; }

    public bool? RansomwareControlsInPlace { get; init; }

    public bool? PriorBreach { get; init; }
}
