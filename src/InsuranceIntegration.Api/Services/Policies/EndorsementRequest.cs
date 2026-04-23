namespace InsuranceIntegration.Api.Services.Policies;

public sealed class EndorsementRequest
{
    public required string PolicyReference { get; init; }

    public required decimal CurrentAnnualPremium { get; init; }

    public required decimal NewAnnualPremium { get; init; }

    public required DateOnly InceptionDate { get; init; }

    public required DateOnly ExpiryDate { get; init; }

    public required DateOnly EffectiveDate { get; init; }
}
