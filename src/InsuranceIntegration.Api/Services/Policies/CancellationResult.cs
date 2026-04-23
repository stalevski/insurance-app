namespace InsuranceIntegration.Api.Services.Policies;

public sealed class CancellationResult
{
    public string PolicyReference { get; init; } = string.Empty;

    public decimal EarnedPremium { get; init; }

    public decimal UnearnedPremium { get; init; }

    public decimal ReturnPremium { get; init; }

    public decimal ShortRatePenalty { get; init; }

    public decimal RetainedPremium { get; init; }

    public string Basis { get; init; } = string.Empty;

    public List<string> Reasons { get; init; } = [];
}
