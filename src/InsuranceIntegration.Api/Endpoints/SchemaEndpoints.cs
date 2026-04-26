using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Schemas;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Endpoints;

public static class SchemaEndpoints
{
    public static IEndpointRouteBuilder MapSchemaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/schemas/ingest/envelope", (IJsonSchemaService schemaService) =>
        {
            return Results.Ok(schemaService.GenerateSchema<SourceIngestEnvelope>());
        });

        endpoints.MapGet("/api/v1/schemas/ingest/risk-request", (IJsonSchemaService schemaService) =>
        {
            return Results.Ok(schemaService.GenerateSchema<SourceIngestRequest>());
        });

        endpoints.MapGet("/api/v1/schemas/canonical/risk-request", (IJsonSchemaService schemaService) =>
        {
            return Results.Ok(schemaService.GenerateSchema<CanonicalRiskRequest>());
        });

        endpoints.MapGet("/api/v1/schemas/final/risk-response", (IJsonSchemaService schemaService) =>
        {
            return Results.Ok(schemaService.GenerateSchema<FinalRiskResponse>());
        });

        return endpoints;
    }
}
