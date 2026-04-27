namespace InsuranceIntegration.Api.Services.Policies;

public sealed class RenewalRequest
{
    /// <summary>
    /// Policy reference being renewed. Must be Bound.
    /// </summary>
    public required string PolicyReference { get; init; }

    /// <summary>
    /// Reference for the new renewal quote. Must be unique across the system.
    /// </summary>
    public required string NewQuoteReference { get; init; }

    public required DateOnly NewInceptionDate { get; init; }

    public required DateOnly NewExpiryDate { get; init; }

    /// <summary>
    /// Annual premium of the prior term (the premium that the loss ratio is computed against).
    /// </summary>
    public required decimal PriorAnnualPremium { get; init; }

    /// <summary>
    /// Sum of indemnity / claim payments incurred during the prior term.
    /// </summary>
    public required decimal PriorClaimsPaid { get; init; }

    /// <summary>
    /// Broker-supplied change in exposure (e.g. revenue) since the prior term.
    /// 0.10 means +10%; -0.05 means -5%. The renewal pricing applies half of this
    /// directly to premium (so +10% revenue -> +5% load).
    /// </summary>
    public decimal RevenueDeltaPercent { get; init; }

    /// <summary>
    /// Optional underwriter override applied on top of the loss-ratio + exposure
    /// loadings. 0.10 = additional +10%, -0.05 = additional -5%.
    /// </summary>
    public decimal? OverrideLoadPercent { get; init; }
}
