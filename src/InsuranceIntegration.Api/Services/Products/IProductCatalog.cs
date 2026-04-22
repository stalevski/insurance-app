namespace InsuranceIntegration.Api.Services.Products;

public interface IProductCatalog
{
    IReadOnlyCollection<ProductDefinition> GetProducts();

    ProductDefinition? FindByProductCode(string productCode);
}
