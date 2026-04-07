namespace InsuranceIntegration.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new
        {
            Status = "Healthy",
            Service = "InsuranceIntegration.Api",
            Framework = $".NET {Environment.Version}"
        }));

        return endpoints;
    }
}
