namespace InsuranceIntegration.Api.Services.Policies;

public sealed class EndorsementResult
{
    public string PolicyReference { get; init; } = string.Empty;

    public decimal PremiumDelta { get; init; }

    public decimal ProRataAdjustment { get; init; }

    public string AdjustmentDirection { get; init; } = string.Empty;

    public decimal SumInsuredDelta { get; init; }

    public decimal DeductibleDelta { get; init; }

    public List<string> OperationsApplied { get; init; } = [];

    public List<string> Reasons { get; init; } = [];
}
