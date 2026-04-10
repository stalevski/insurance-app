namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class QuoteData
{
    public string? QuoteReference { get; init; }

    public DateOnly? EffectiveDate { get; init; }

    public DateOnly? ExpiryDate { get; init; }

    public string? QuoteStatusHint { get; init; }
}
