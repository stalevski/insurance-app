using InsuranceIntegration.Api.Services.Products;

namespace InsuranceIntegration.Api.Services.Pricing;

public sealed class RatingService : IRatingService
{
    private readonly IProductCatalog _productCatalog;

    public RatingService(IProductCatalog productCatalog)
    {
        _productCatalog = productCatalog;
    }

    public RatingQuote Rate(string productCode, decimal annualRevenue, string currencyCode)
    {
        var product = _productCatalog.FindByProductCode(productCode)
            ?? throw new InvalidOperationException($"Product '{productCode}' is not defined in the catalog.");

        var reasons = new List<string>();
        var revenueThousands = annualRevenue > 0m ? annualRevenue / 1000m : 0m;
        var revenuePremium = Math.Round(revenueThousands * product.BaseRatePerThousandRevenue, 2, MidpointRounding.AwayFromZero);
        reasons.Add($"Revenue-based premium: {revenuePremium:0.##} ({revenueThousands:0.##} x {product.BaseRatePerThousandRevenue})");

        decimal largeAccountLoad = 0m;
        if (annualRevenue >= product.LargeAccountThreshold)
        {
            largeAccountLoad = Math.Round(revenuePremium * product.LargeAccountLoad, 2, MidpointRounding.AwayFromZero);
            reasons.Add($"Large account load applied: {largeAccountLoad:0.##}");
        }

        var calculated = revenuePremium + largeAccountLoad;
        var appliedMinimum = Math.Max(calculated, product.MinimumPremium);
        if (appliedMinimum > calculated)
        {
            reasons.Add($"Minimum premium applied: {product.MinimumPremium:0.##}");
        }

        return new RatingQuote
        {
            ProductCode = product.ProductCode,
            BaseRate = product.BaseRatePerThousandRevenue,
            RevenueBasedPremium = revenuePremium,
            AppliedMinimumPremium = product.MinimumPremium,
            LargeAccountLoadAmount = largeAccountLoad,
            TechnicalPremium = appliedMinimum,
            CurrencyCode = currencyCode,
            RatingReasons = reasons
        };
    }
}
