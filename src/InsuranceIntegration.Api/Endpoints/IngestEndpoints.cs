using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Endpoints;

public static class IngestEndpoints
{
    public static IEndpointRouteBuilder MapIngestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/ingest", (SourceIngestEnvelope envelope, IIngestDispatcher dispatcher) =>
        {
            var result = dispatcher.Dispatch(envelope);
            return Results.Ok(result);
        });

        endpoints.MapPost("/api/v1/ingest/risks", (SourceIngestRequest request, IRiskIngestMapper mapper, IRiskFlowService riskFlowService) =>
        {
            var canonicalRequest = mapper.Map(request);
            var response = riskFlowService.Process(canonicalRequest);
            return Results.Ok(response);
        });

        return endpoints;
    }
}
