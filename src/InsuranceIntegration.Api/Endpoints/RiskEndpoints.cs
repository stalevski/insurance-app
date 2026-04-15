using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Services.Flows;

namespace InsuranceIntegration.Api.Endpoints;

public static class RiskEndpoints
{
    public static IEndpointRouteBuilder MapRiskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/risks", (CanonicalRiskRequest request, IRiskFlowService riskFlowService) =>
        {
            var response = riskFlowService.Process(request);
            return Results.Ok(response);
        });

        return endpoints;
    }
}
