namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class PerilData
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool IsCovered { get; init; } = true;

    public decimal? SubLimit { get; init; }

    public int WaitingPeriodDays { get; init; }
}
