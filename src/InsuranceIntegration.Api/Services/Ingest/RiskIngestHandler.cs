using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.Services.Orchestration;
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
    private readonly IRiskSubmissionOrchestrator _orchestrator;
    private readonly TimeProvider _timeProvider;

    public RiskIngestHandler(
        IRiskIngestMapper riskIngestMapper,
        IRiskSubmissionOrchestrator orchestrator,
        TimeProvider timeProvider)
    {
        _riskIngestMapper = riskIngestMapper;
        _orchestrator = orchestrator;
        _timeProvider = timeProvider;
    }

    public string Name => "RiskIngestHandler";

    public bool CanHandle(SourceIngestEnvelope envelope)
    {
        return SupportedTypes.Contains(envelope.Type);
    }

    public async Task<object> HandleAsync(SourceIngestEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var request = new SourceIngestRequest
        {
            SourceSystem = envelope.Source,
            MessageType = envelope.Type,
            Payload = envelope.Data
        };

        var canonicalRequest = _riskIngestMapper.Map(request);

        var ingestContext = new IngestContext
        {
            Source = envelope.Source,
            EnvelopeId = envelope.Id,
            MessageType = envelope.Type,
            CorrelationId = envelope.CorrelationId,
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

        var response = await _orchestrator.HandleAsync(canonicalRequest, ingestContext, cancellationToken);
        return response;
    }
}
