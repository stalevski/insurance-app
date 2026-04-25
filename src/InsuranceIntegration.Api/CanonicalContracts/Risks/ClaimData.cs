namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class ClaimData
{
    public string ClaimReference { get; init; } = string.Empty;

    public string ClaimantName { get; init; } = string.Empty;

    public decimal IncurredAmount { get; init; }

    public decimal ReservedAmount { get; init; }

    public string? AffectedSectionCode { get; init; }

    public string? AffectedSubcoverCode { get; init; }

    public string? AffectedPerilCode { get; init; }
}
