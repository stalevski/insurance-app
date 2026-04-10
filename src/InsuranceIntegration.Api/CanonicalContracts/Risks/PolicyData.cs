namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class PolicyData
{
    public string? PolicyReference { get; init; }

    public DateOnly? InceptionDate { get; init; }

    public DateOnly? ExpiryDate { get; init; }
}
