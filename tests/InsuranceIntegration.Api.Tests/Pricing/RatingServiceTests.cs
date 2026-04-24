using InsuranceIntegration.Api.Services.Pricing;
using InsuranceIntegration.Api.Services.Products;

namespace InsuranceIntegration.Api.Tests.Pricing;

public sealed class RatingServiceTests
{
    [Test]
    public void Rate_AppliesMinimumPremiumForSmallAccounts()
    {
        var service = new RatingService(new ProductCatalog());

        var result = service.Rate("COMMERCIAL_PROPERTY", 10_000m, "USD");

        Assert.That(result.TechnicalPremium, Is.EqualTo(750m));
        Assert.That(result.AppliedMinimumPremium, Is.EqualTo(750m));
    }

    [Test]
    public void Rate_AppliesLargeAccountLoadAboveThreshold()
    {
        var service = new RatingService(new ProductCatalog());

        var result = service.Rate("COMMERCIAL_PROPERTY", 20_000_000m, "USD");

        Assert.That(result.LargeAccountLoadAmount, Is.GreaterThan(0m));
        Assert.That(result.TechnicalPremium, Is.GreaterThan(result.RevenueBasedPremium));
    }

    [Test]
    public void Rate_ThrowsForUnknownProduct()
    {
        var service = new RatingService(new ProductCatalog());

        var exception = Assert.Throws<InvalidOperationException>(() => service.Rate("UNKNOWN", 1_000_000m, "USD"));
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("not defined in the catalog"));
    }
}
