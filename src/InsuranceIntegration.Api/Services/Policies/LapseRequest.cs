namespace InsuranceIntegration.Api.Services.Policies;

/// <summary>
/// Lapses an in-force policy, typically because a due premium installment was never paid.
/// Cover is provided up to the lapse date (the insured owes the pro-rata earned premium);
/// the remaining term is forfeited and not refunded.
/// </summary>
public sealed class LapseRequest
{
    public required string PolicyReference { get; init; }

    public required decimal AnnualPremium { get; init; }

    public required DateOnly InceptionDate { get; init; }

    public required DateOnly ExpiryDate { get; init; }

    /// <summary>Date the policy lapses (cover ceases). Clamped to the policy period.</summary>
    public required DateOnly LapseDate { get; init; }

    /// <summary>Premium actually received from the insured before the lapse.</summary>
    public decimal PaidToDate { get; init; }

    public string? Reason { get; init; }
}
