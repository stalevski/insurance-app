using InsuranceIntegration.Api.Services.Catalog;

namespace InsuranceIntegration.Api.Endpoints;

public static class SourceSystemEndpoints
{
    public static IEndpointRouteBuilder MapSourceSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/source-systems", (ISourceSystemCatalogService catalogService) =>
        {
            return Results.Ok(catalogService.GetSourceSystems());
        });

        return endpoints;
    }
}
