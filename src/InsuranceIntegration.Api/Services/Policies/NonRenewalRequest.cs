namespace InsuranceIntegration.Api.Services.Policies;

/// <summary>
/// Records a non-renewal: an in-force policy is allowed to run to its natural expiry and is
/// deliberately not renewed for a subsequent term. There is no mid-term premium adjustment.
/// </summary>
public sealed class NonRenewalRequest
{
    public required string PolicyReference { get; init; }

    public required decimal AnnualPremium { get; init; }

    public required DateOnly InceptionDate { get; init; }

    /// <summary>The current term's expiry date, on which cover is not renewed.</summary>
    public required DateOnly ExpiryDate { get; init; }

    /// <summary>Who decided not to renew (insurer or insured).</summary>
    public string InitiatedBy { get; init; } = NonRenewalInitiator.Insurer;

    /// <summary>Days of advance notice given to the policyholder before expiry.</summary>
    public int NoticeDays { get; init; }

    public string? Reason { get; init; }
}
