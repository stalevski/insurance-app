using InsuranceIntegration.Api.Services.Snapshots;

namespace InsuranceIntegration.Api.Endpoints;

public static class QuoteReadEndpoints
{
    public static IEndpointRouteBuilder MapQuoteReadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/quotes", (IQuoteSnapshotService service, int? skip, int? take) =>
        {
            var snapshots = service.List(skip ?? 0, take ?? 100);
            return Results.Ok(new { items = snapshots, count = snapshots.Count });
        });

        endpoints.MapGet("/api/v1/quotes/{quoteReference}", (string quoteReference, IQuoteSnapshotService service) =>
        {
            var snapshot = service.Find(quoteReference);
            return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
        });

        endpoints.MapPost("/api/v1/snapshots/quotes/{quoteReference}/rebuild", (string quoteReference, ISnapshotRebuildService rebuild) =>
        {
            var result = rebuild.RebuildQuote(quoteReference);
            return result.EventsApplied == 0 ? Results.NotFound() : Results.Ok(result);
        });

        return endpoints;
    }
}
