namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class EnrichmentItem
{
    public string Family { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal Multiplier { get; init; } = 1m;

    public bool IsBlocking { get; init; }

    public bool IsDerived { get; init; }
}
