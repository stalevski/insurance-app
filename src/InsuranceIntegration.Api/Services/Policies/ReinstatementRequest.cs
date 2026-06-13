namespace InsuranceIntegration.Api.Services.Policies;

public sealed class ReinstatementRequest
{
    public required string PolicyReference { get; init; }

    public required decimal AnnualPremium { get; init; }

    public required DateOnly InceptionDate { get; init; }

    public required DateOnly ExpiryDate { get; init; }

    /// <summary>Date the policy was cancelled / lapsed.</summary>
    public required DateOnly CancellationDate { get; init; }

    /// <summary>Date cover resumes.</summary>
    public required DateOnly ReinstatementDate { get; init; }

    /// <summary>Flat administrative fee charged to reinstate the policy.</summary>
    public decimal ReinstatementFee { get; init; } = 0m;

    /// <summary>
    /// When true, the lapsed period is treated as covered and the insured is charged the
    /// pro-rata premium for the gap (continuous cover). When false, the gap is uncovered and
    /// the lapsed premium is deducted from the policy's annual premium instead of charged.
    /// </summary>
    public bool ChargeLapsedPremium { get; init; } = false;

    public string? Reason { get; init; }
}
