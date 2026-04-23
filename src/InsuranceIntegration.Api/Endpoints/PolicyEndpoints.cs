using InsuranceIntegration.Api.Services.Policies;

namespace InsuranceIntegration.Api.Endpoints;

public static class PolicyEndpoints
{
    public static IEndpointRouteBuilder MapPolicyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/policies/cancellations", (CancellationRequest request, IPolicyAdjustmentService service) =>
        {
            var result = service.CalculateCancellation(request);
            return Results.Ok(result);
        });

        endpoints.MapPost("/api/v1/policies/endorsements", (EndorsementRequest request, IPolicyAdjustmentService service) =>
        {
            var result = service.CalculateEndorsement(request);
            return Results.Ok(result);
        });

        return endpoints;
    }
}
