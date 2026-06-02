using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Endpoints;

public static class IngestEndpoints
{
    public static IEndpointRouteBuilder MapIngestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/ingest", async (SourceIngestEnvelope envelope, IIngestDispatcher dispatcher, CancellationToken ct) =>
        {
            var receipt = await dispatcher.DispatchAsync(envelope, ct);
            return Results.Ok(receipt);
        });

        endpoints.MapPost("/api/v1/ingest/risks", (SourceIngestRequest request, IRiskIngestMapper mapper, IRiskFlowService riskFlowService) =>
        {
            var canonicalRequest = mapper.Map(request);
            var response = riskFlowService.Process(canonicalRequest);
            return Results.Ok(response);
        });

        endpoints.MapGet("/api/v1/ingest/{source}/{envelopeId}", async (string source, string envelopeId, IIdempotencyStore store, CancellationToken ct) =>
        {
            var receipt = await store.FindAsync(source, envelopeId, ct);
            return receipt is null ? Results.NotFound() : Results.Ok(receipt);
        });

        return endpoints;
    }
}
