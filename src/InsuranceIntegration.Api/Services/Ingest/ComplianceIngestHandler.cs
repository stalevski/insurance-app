using System.Text.Json;
using InsuranceIntegration.Api.CanonicalContracts.Compliance;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.SourceContracts.Compliance;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public sealed class ComplianceIngestHandler : IIngestHandler
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ComplianceResult",
        "FraudAssessment"
    };

    private readonly IComplianceFlowService _complianceFlowService;

    public ComplianceIngestHandler(IComplianceFlowService complianceFlowService)
    {
        _complianceFlowService = complianceFlowService;
    }

    public string Name => "ComplianceIngestHandler";

    public bool CanHandle(SourceIngestEnvelope envelope)
    {
        return SupportedTypes.Contains(envelope.Type);
    }

    public Task<object> HandleAsync(SourceIngestEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var payload = envelope.Data.Deserialize<ComplianceResultPayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Unable to deserialize compliance payload.");

        var request = new CanonicalComplianceRequest
        {
            EntityId = Guid.NewGuid(),
            PartyName = payload.PartyName,
            SourceSystem = envelope.Source,
            EntityReference = payload.EntityReference,
            ScreeningResult = payload.ScreeningResult,
            Score = payload.Score,
            IsPoliticallyExposed = payload.IsPoliticallyExposed,
            HasSanctionsHit = payload.HasSanctionsHit
        };

        return Task.FromResult<object>(_complianceFlowService.Process(request));
    }
}
