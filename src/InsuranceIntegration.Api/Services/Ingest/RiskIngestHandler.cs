using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Snapshots;
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
    private readonly IRiskSnapshotRouter _snapshotRouter;
    private readonly TimeProvider _timeProvider;

    public RiskIngestHandler(
        IRiskIngestMapper riskIngestMapper,
        IRiskFlowService riskFlowService,
        IRiskSnapshotRouter snapshotRouter,
        TimeProvider timeProvider)
    {
        _riskIngestMapper = riskIngestMapper;
        _riskFlowService = riskFlowService;
        _snapshotRouter = snapshotRouter;
        _timeProvider = timeProvider;
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
        var response = _riskFlowService.Process(canonicalRequest);

        var ingestContext = new IngestContext
        {
            Source = envelope.Source,
            EnvelopeId = envelope.Id,
            MessageType = envelope.Type,
            CorrelationId = envelope.CorrelationId,
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        };
        _snapshotRouter.Route(canonicalRequest, response, ingestContext);

        return response;
    }
}
