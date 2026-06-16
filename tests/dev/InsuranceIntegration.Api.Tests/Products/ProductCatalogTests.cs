using InsuranceIntegration.Api.Services.Products;

namespace InsuranceIntegration.Api.Tests.Products;

/// <summary>
/// Unit coverage for the in-memory <see cref="ProductCatalog"/>: the four known products are
/// returned, lookup is case-insensitive, and an unknown product code resolves to <c>null</c>.
/// </summary>
public sealed class ProductCatalogTests
{
    private static readonly string[] ExpectedProductCodes =
        ["COMMERCIAL_PROPERTY", "LIABILITY", "CYBER", "MOTOR"];

    [Test]
    public void GetProducts_ReturnsTheFourKnownProductCodes()
    {
        var catalog = new ProductCatalog();

        var codes = catalog.GetProducts().Select(product => product.ProductCode).ToArray();

        Assert.That(codes, Is.EquivalentTo(ExpectedProductCodes));
    }

    [Test]
    public void FindByProductCode_ResolvesAKnownCode()
    {
        var catalog = new ProductCatalog();

        var product = catalog.FindByProductCode("COMMERCIAL_PROPERTY");

        Assert.That(product, Is.Not.Null);
        Assert.That(product!.DisplayName, Is.EqualTo("Commercial Property"));
    }

    [Test]
    public void FindByProductCode_IsCaseInsensitive()
    {
        var catalog = new ProductCatalog();

        var product = catalog.FindByProductCode("commercial_property");

        Assert.That(product, Is.Not.Null);
        Assert.That(product!.ProductCode, Is.EqualTo("COMMERCIAL_PROPERTY"));
    }

    [Test]
    public void FindByProductCode_ReturnsNull_ForAnUnknownCode()
    {
        var catalog = new ProductCatalog();

        Assert.That(catalog.FindByProductCode("NOT_A_PRODUCT"), Is.Null);
    }
}
