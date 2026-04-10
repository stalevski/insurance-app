namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class ComplianceCheck
{
    public string Code { get; init; } = string.Empty;

    public bool IsComplete { get; init; }

    public string? Description { get; init; }
}
