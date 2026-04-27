namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class SubmissionData
{
    public int UnderwritingYear { get; init; }

    public string? ChannelCode { get; init; }

    public decimal? BrokerPremium { get; init; }

    public decimal? TechnicalPremium { get; init; }

    public decimal? Revenue { get; init; }

    public bool IsRenewal { get; init; }

    /// <summary>
    /// When IsRenewal is true, the policy reference being renewed. Lets downstream
    /// projectors and consumers link the new quote back to its lineage.
    /// </summary>
    public string? PriorPolicyReference { get; init; }
}
