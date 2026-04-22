namespace InsuranceIntegration.Api.Services.Products;

public sealed class ProductCatalog : IProductCatalog
{
    private static readonly IReadOnlyCollection<ProductDefinition> Products = new ProductDefinition[]
    {
        new() { ProductCode = "COMMERCIAL_PROPERTY", DisplayName = "Commercial Property", Family = "Property", BaseRatePerThousandRevenue = 2.50m, MinimumPremium = 750m },
        new() { ProductCode = "LIABILITY", DisplayName = "General Liability", Family = "Liability", BaseRatePerThousandRevenue = 1.80m, MinimumPremium = 500m },
        new() { ProductCode = "CYBER", DisplayName = "Cyber Liability", Family = "Cyber", BaseRatePerThousandRevenue = 4.00m, MinimumPremium = 1500m },
        new() { ProductCode = "MOTOR", DisplayName = "Commercial Motor", Family = "Auto", BaseRatePerThousandRevenue = 3.20m, MinimumPremium = 900m }
    };

    public IReadOnlyCollection<ProductDefinition> GetProducts()
    {
        return Products;
    }

    public ProductDefinition? FindByProductCode(string productCode)
    {
        return Products.FirstOrDefault(product => string.Equals(product.ProductCode, productCode, StringComparison.OrdinalIgnoreCase));
    }
}
