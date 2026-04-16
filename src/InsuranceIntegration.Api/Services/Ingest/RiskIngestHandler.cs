using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public sealed class RiskIngestHandler : IIngestHandler
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "RiskSubmission",
        "QuoteRequest",
        "PolicyBindRequest"
    };

    private readonly IRiskIngestMapper _riskIngestMapper;
    private readonly IRiskFlowService _riskFlowService;

    public RiskIngestHandler(IRiskIngestMapper riskIngestMapper, IRiskFlowService riskFlowService)
    {
        _riskIngestMapper = riskIngestMapper;
        _riskFlowService = riskFlowService;
    }

    public string Name => "RiskIngestHandler";

    public bool CanHandle(SourceIngestEnvelope envelope)
    {
        return SupportedTypes.Contains(envelope.Type);
    }

    public object Handle(SourceIngestEnvelope envelope)
    {
        var request = new SourceIngestRequest
        {
            SourceSystem = envelope.Source,
            MessageType = envelope.Type,
            Payload = envelope.Data
        };

        var canonicalRequest = _riskIngestMapper.Map(request);
        return _riskFlowService.Process(canonicalRequest);
    }
}
