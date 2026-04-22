using InsuranceIntegration.Api.Services.Pricing;
using InsuranceIntegration.Api.Services.Products;

namespace InsuranceIntegration.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/products", (IProductCatalog catalog) => Results.Ok(catalog.GetProducts()));

        endpoints.MapGet("/api/v1/products/{productCode}/rating", (string productCode, decimal annualRevenue, string? currencyCode, IRatingService ratingService) =>
        {
            var result = ratingService.Rate(productCode, annualRevenue, currencyCode ?? "USD");
            return Results.Ok(result);
        });

        return endpoints;
    }
}
