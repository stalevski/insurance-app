using InsuranceIntegration.Api.Services.Snapshots;

namespace InsuranceIntegration.Api.Endpoints;

public static class PolicyReadEndpoints
{
    public static IEndpointRouteBuilder MapPolicyReadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/policies", (IPolicySnapshotService service, int? skip, int? take) =>
        {
            var snapshots = service.List(skip ?? 0, take ?? 100);
            return Results.Ok(new { items = snapshots, count = snapshots.Count });
        });

        endpoints.MapGet("/api/v1/policies/{policyReference}", (string policyReference, IPolicySnapshotService service) =>
        {
            var snapshot = service.Find(policyReference);
            return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
        });

        endpoints.MapPost("/api/v1/snapshots/policies/{policyReference}/rebuild", (string policyReference, ISnapshotRebuildService rebuild) =>
        {
            var result = rebuild.RebuildPolicy(policyReference);
            return result.EventsApplied == 0 ? Results.NotFound() : Results.Ok(result);
        });

        return endpoints;
    }
}
