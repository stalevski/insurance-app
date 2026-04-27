namespace InsuranceIntegration.Api.Services.Policies;

public sealed class RenewalResult
{
    public required string PriorPolicyReference { get; init; }

    public required string NewQuoteReference { get; init; }

    public required decimal PriorAnnualPremium { get; init; }

    public required decimal LossRatio { get; init; }

    /// <summary>
    /// Excellent (&lt; 30%), Standard (30-60%), Loaded (60-80%),
    /// HeavilyLoaded (80-100%), Distressed (&gt; 100%).
    /// </summary>
    public required string LossRatioBand { get; init; }

    public required decimal LossRatioLoadPercent { get; init; }

    public required decimal ExposureLoadPercent { get; init; }

    public required decimal OverrideLoadPercent { get; init; }

    public required decimal RenewalPremium { get; init; }

    public required Guid PolicyRenewedEventId { get; init; }

    public required Guid QuoteIssuedEventId { get; init; }

    public required List<string> Reasons { get; init; }
}
