namespace InsuranceIntegration.Api.Services.Products;

public sealed class ProductDefinition
{
    public string ProductCode { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Family { get; init; } = string.Empty;

    public decimal BaseRatePerThousandRevenue { get; init; }

    public decimal MinimumPremium { get; init; }

    public decimal LargeAccountThreshold { get; init; } = 10_000_000m;

    public decimal LargeAccountLoad { get; init; } = 0.05m;
}
