namespace InsuranceIntegration.Api.Services.Policies;

public sealed class EndorsementResult
{
    public string PolicyReference { get; init; } = string.Empty;

    public decimal PremiumDelta { get; init; }

    public decimal ProRataAdjustment { get; init; }

    public string AdjustmentDirection { get; init; } = string.Empty;

    public List<string> Reasons { get; init; } = [];
}
