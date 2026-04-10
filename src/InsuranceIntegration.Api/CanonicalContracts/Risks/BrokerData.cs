namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class BrokerData
{
    public string? BrokerCode { get; init; }

    public string? BrokerName { get; init; }

    public bool HasDelegatedAuthority { get; init; }

    public bool IsPreferredPartner { get; init; }
}
