using InsuranceIntegration.Api.Services.Pricing;

namespace InsuranceIntegration.Api.IntegrationTests.Builders;

/// <summary>
/// An independent oracle for the product rating logic. It re-derives the expected premium for a
/// product and annual revenue from first principles — deliberately NOT calling the production
/// <see cref="RatingService"/> — so a test can assert that the running API produces the same numbers.
/// If the production formula or catalog ever drifts, <see cref="AssertMatches"/> fails and pinpoints
/// the diverging field.
/// </summary>
/// <remarks>
/// Mirrors the published catalog and the documented formula:
/// <c>revenuePremium = round(revenue / 1000 × baseRate, 2)</c>;
/// a <c>5%</c> large-account load applies when <c>revenue ≥ 10,000,000</c>;
/// the technical premium is the greater of the loaded premium and the product minimum.
/// </remarks>
public sealed class ExpectedRatingResult
{
    private static readonly Dictionary<string, CatalogEntry> Catalog =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["COMMERCIAL_PROPERTY"] = new("COMMERCIAL_PROPERTY", 2.50m, 750m),
            ["LIABILITY"] = new("LIABILITY", 1.80m, 500m),
            ["CYBER"] = new("CYBER", 4.00m, 1_500m),
            ["MOTOR"] = new("MOTOR", 3.20m, 900m),
        };

    private const decimal LargeAccountThreshold = 10_000_000m;
    private const decimal LargeAccountLoad = 0.05m;

    private readonly CatalogEntry _entry;
    private decimal _annualRevenue;
    private string _currencyCode = "USD";

    private ExpectedRatingResult(CatalogEntry entry)
    {
        _entry = entry;
    }

    /// <summary>Starts an expectation for a known catalog product (case-insensitive).</summary>
    public static ExpectedRatingResult ForProduct(string productCode)
    {
        if (!Catalog.TryGetValue(productCode, out var entry))
        {
            throw new ArgumentException($"'{productCode}' is not a known catalog product.", nameof(productCode));
        }

        return new ExpectedRatingResult(entry);
    }

    public ExpectedRatingResult WithAnnualRevenue(decimal annualRevenue)
    {
        _annualRevenue = annualRevenue;
        return this;
    }

    public ExpectedRatingResult InCurrency(string currencyCode)
    {
        _currencyCode = currencyCode;
        return this;
    }

    public string ProductCode => _entry.ProductCode;

    public decimal BaseRate => _entry.BaseRatePerThousand;

    public decimal AppliedMinimumPremium => _entry.MinimumPremium;

    public string CurrencyCode => _currencyCode;

    public decimal RevenueBasedPremium
    {
        get
        {
            var revenueThousands = _annualRevenue > 0m ? _annualRevenue / 1000m : 0m;
            return Math.Round(revenueThousands * _entry.BaseRatePerThousand, 2, MidpointRounding.AwayFromZero);
        }
    }

    public decimal LargeAccountLoadAmount =>
        _annualRevenue >= LargeAccountThreshold
            ? Math.Round(RevenueBasedPremium * LargeAccountLoad, 2, MidpointRounding.AwayFromZero)
            : 0m;

    public decimal TechnicalPremium => Math.Max(RevenueBasedPremium + LargeAccountLoadAmount, _entry.MinimumPremium);

    /// <summary>True when the product minimum lifts the technical premium above the calculated amount.</summary>
    public bool MinimumApplied => TechnicalPremium > RevenueBasedPremium + LargeAccountLoadAmount;

    /// <summary>Asserts every field of an actual <see cref="RatingQuote"/> matches this expectation.</summary>
    public void AssertMatches(RatingQuote actual)
    {
        ArgumentNullException.ThrowIfNull(actual);

        Assert.Multiple(() =>
        {
            Assert.That(actual.ProductCode, Is.EqualTo(ProductCode), "ProductCode");
            Assert.That(actual.BaseRate, Is.EqualTo(BaseRate), "BaseRate");
            Assert.That(actual.RevenueBasedPremium, Is.EqualTo(RevenueBasedPremium), "RevenueBasedPremium");
            Assert.That(actual.AppliedMinimumPremium, Is.EqualTo(AppliedMinimumPremium), "AppliedMinimumPremium");
            Assert.That(actual.LargeAccountLoadAmount, Is.EqualTo(LargeAccountLoadAmount), "LargeAccountLoadAmount");
            Assert.That(actual.TechnicalPremium, Is.EqualTo(TechnicalPremium), "TechnicalPremium");
            Assert.That(actual.CurrencyCode, Is.EqualTo(CurrencyCode), "CurrencyCode");
            Assert.That(actual.RatingReasons, Is.Not.Empty, "RatingReasons should explain the calculation.");
        });
    }

    private sealed record CatalogEntry(string ProductCode, decimal BaseRatePerThousand, decimal MinimumPremium);
}
