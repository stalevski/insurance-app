namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class InsuredData
{
    public string? FullName { get; init; }

    public string? TradingName { get; init; }

    public string? SegmentCode { get; init; }

    public decimal? AnnualRevenue { get; init; }

    public int EmployeeCount { get; init; }

    public int YearsInBusiness { get; init; }
}
