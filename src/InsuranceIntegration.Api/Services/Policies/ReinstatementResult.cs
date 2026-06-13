namespace InsuranceIntegration.Api.Services.Policies;

public sealed class ReinstatementResult
{
    public string PolicyReference { get; init; } = string.Empty;

    public int LapsedDays { get; init; }

    public decimal ReinstatementFee { get; init; }

    /// <summary>Pro-rata premium attributable to the lapsed period.</summary>
    public decimal LapsedPremium { get; init; }

    /// <summary>Total amount the insured must pay to reinstate (fee plus any charged lapsed premium).</summary>
    public decimal AmountDueOnReinstatement { get; init; }

    /// <summary>The policy's annual premium after reinstatement.</summary>
    public decimal ReinstatedAnnualPremium { get; init; }

    /// <summary>True when the lapsed period remains uncovered (a gap in cover).</summary>
    public bool GapInCoverage { get; init; }

    public List<string> Reasons { get; init; } = [];
}
