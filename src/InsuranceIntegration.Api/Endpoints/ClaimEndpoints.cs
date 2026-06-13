using InsuranceIntegration.Api.Services.Claims;

namespace InsuranceIntegration.Api.Endpoints;

public static class ClaimEndpoints
{
    public static IEndpointRouteBuilder MapClaimEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/claims/transitions", (ClaimTransitionRequest request, IClaimLifecycleService claims) =>
        {
            try
            {
                var result = claims.Transition(request);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid claim transition request");
            }
        });

        endpoints.MapPost("/api/v1/claims/financials", (ClaimFinancialRequest request, IClaimFinancialService financials) =>
        {
            try
            {
                var result = financials.Apply(request);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid claim financial request");
            }
        });

        return endpoints;
    }
}
