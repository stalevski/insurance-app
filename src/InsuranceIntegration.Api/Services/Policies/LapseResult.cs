namespace InsuranceIntegration.Api.Services.Policies;

public sealed class LapseResult
{
    public string PolicyReference { get; init; } = string.Empty;

    /// <summary>Days of cover provided from inception to the lapse date.</summary>
    public int CoveredDays { get; init; }

    /// <summary>Pro-rata premium earned for the covered period (what the insured owes for cover provided).</summary>
    public decimal EarnedPremium { get; init; }

    /// <summary>Premium for the forfeited post-lapse period (not refunded, not earned).</summary>
    public decimal UnearnedPremium { get; init; }

    /// <summary>Earned premium not yet paid by the insured (the collectible shortfall at lapse).</summary>
    public decimal OutstandingPremium { get; init; }

    public List<string> Reasons { get; init; } = [];
}
