namespace InsuranceIntegration.Api.Services.Policies;

public sealed class NonRenewalResult
{
    public string PolicyReference { get; init; } = string.Empty;

    /// <summary>Date the non-renewal takes effect (the current term's expiry date).</summary>
    public DateOnly EffectiveDate { get; init; }

    public string InitiatedBy { get; init; } = NonRenewalInitiator.Insurer;

    public int NoticeDays { get; init; }

    public List<string> Reasons { get; init; } = [];
}
