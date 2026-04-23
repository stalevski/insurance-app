namespace InsuranceIntegration.Api.Services.Policies;

public sealed class CancellationRequest
{
    public required string PolicyReference { get; init; }

    public required decimal AnnualPremium { get; init; }

    public required DateOnly InceptionDate { get; init; }

    public required DateOnly ExpiryDate { get; init; }

    public required DateOnly CancellationDate { get; init; }

    public string Basis { get; init; } = CancellationBasis.ProRata;

    public decimal ShortRatePenaltyPercent { get; init; } = 0.10m;

    public decimal MinimumRetainedPremium { get; init; } = 0m;
}
