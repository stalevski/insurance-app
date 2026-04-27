using InsuranceIntegration.Api.Services.Policies;

namespace InsuranceIntegration.Api.Endpoints;

public static class PolicyEndpoints
{
    public static IEndpointRouteBuilder MapPolicyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/policies/cancellations", (CancellationRequest request, IPolicyLifecycleService lifecycle) =>
        {
            try
            {
                var result = lifecycle.ApplyCancellation(request);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Policy not found");
            }
            catch (ArgumentException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid cancellation request");
            }
        });

        endpoints.MapPost("/api/v1/policies/endorsements", (EndorsementRequest request, IPolicyLifecycleService lifecycle) =>
        {
            try
            {
                var result = lifecycle.ApplyEndorsement(request);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Policy not found");
            }
            catch (ArgumentException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid endorsement request");
            }
        });

        return endpoints;
    }
}
