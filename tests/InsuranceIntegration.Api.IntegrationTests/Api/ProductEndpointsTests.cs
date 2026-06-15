using InsuranceIntegration.Api.IntegrationTests.Builders;
using InsuranceIntegration.Api.IntegrationTests.Infrastructure;
using InsuranceIntegration.Api.Services.Pricing;
using InsuranceIntegration.Api.Services.Products;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// Read coverage for the product catalog and the rating endpoint. The rating assertions are driven
/// by <see cref="ExpectedRatingResult"/>, an independent re-implementation of the premium formula,
/// so the test fails if the running API ever diverges from the documented logic.
/// </summary>
public sealed class ProductEndpointsTests : ApiTestBase
{
    private static readonly string[] ExpectedProductCodes =
        ["COMMERCIAL_PROPERTY", "LIABILITY", "CYBER", "MOTOR"];

    [Test]
    public async Task GetProducts_ReturnsTheFullCatalog()
    {
        using var response = await GetAsync("/api/v1/products");

        var products = await response.ShouldReturnAsync<List<ProductDefinition>>();
        Assert.Multiple(() =>
        {
            Assert.That(products, Has.Count.EqualTo(4));
            Assert.That(
                products.Select(product => product.ProductCode),
                Is.EquivalentTo(ExpectedProductCodes));
        });
    }

    [TestCase("COMMERCIAL_PROPERTY", 4_000_000)]
    [TestCase("LIABILITY", 2_500_000)]
    [TestCase("CYBER", 6_000_000)]
    [TestCase("MOTOR", 1_800_000)]
    public async Task GetRating_MatchesTheExpectedPremiumOracle(string productCode, int annualRevenue)
    {
        var expected = ExpectedRatingResult.ForProduct(productCode).WithAnnualRevenue(annualRevenue);

        using var response = await GetAsync(
            $"/api/v1/products/{productCode}/rating?annualRevenue={annualRevenue}");

        var actual = await response.ShouldReturnAsync<RatingQuote>();
        expected.AssertMatches(actual);
    }

    [Test]
    public async Task GetRating_AppliesMinimumPremium_ForSmallRevenue()
    {
        var expected = ExpectedRatingResult.ForProduct("COMMERCIAL_PROPERTY").WithAnnualRevenue(10_000);

        using var response = await GetAsync(
            "/api/v1/products/COMMERCIAL_PROPERTY/rating?annualRevenue=10000");

        var actual = await response.ShouldReturnAsync<RatingQuote>();
        Assert.Multiple(() =>
        {
            Assert.That(expected.MinimumApplied, Is.True, "Scenario should exercise the minimum-premium floor.");
            Assert.That(actual.TechnicalPremium, Is.EqualTo(750m));
        });
        expected.AssertMatches(actual);
    }

    [Test]
    public async Task GetRating_AddsLargeAccountLoad_AtThreshold()
    {
        var expected = ExpectedRatingResult.ForProduct("CYBER").WithAnnualRevenue(12_000_000);

        using var response = await GetAsync(
            "/api/v1/products/CYBER/rating?annualRevenue=12000000");

        var actual = await response.ShouldReturnAsync<RatingQuote>();
        Assert.Multiple(() =>
        {
            Assert.That(expected.LargeAccountLoadAmount, Is.GreaterThan(0m), "Scenario should exercise the large-account load.");
            Assert.That(actual.LargeAccountLoadAmount, Is.GreaterThan(0m));
        });
        expected.AssertMatches(actual);
    }

    [Test]
    public async Task GetRating_HonoursCurrencyCode()
    {
        var expected = ExpectedRatingResult.ForProduct("MOTOR").WithAnnualRevenue(3_000_000).InCurrency("EUR");

        using var response = await GetAsync(
            "/api/v1/products/MOTOR/rating?annualRevenue=3000000&currencyCode=EUR");

        var actual = await response.ShouldReturnAsync<RatingQuote>();
        Assert.That(actual.CurrencyCode, Is.EqualTo("EUR"));
        expected.AssertMatches(actual);
    }
}
