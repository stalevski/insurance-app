namespace InsuranceIntegration.Api.Services.Pricing;

public sealed class RatingQuote
{
    public string ProductCode { get; init; } = string.Empty;

    public decimal BaseRate { get; init; }

    public decimal RevenueBasedPremium { get; init; }

    public decimal AppliedMinimumPremium { get; init; }

    public decimal LargeAccountLoadAmount { get; init; }

    public decimal TechnicalPremium { get; init; }

    public string CurrencyCode { get; init; } = "USD";

    public List<string> RatingReasons { get; init; } = [];
}
