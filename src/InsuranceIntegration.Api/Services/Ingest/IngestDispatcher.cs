using InsuranceIntegration.Api.Responses.Ingest;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public sealed class IngestDispatcher : IIngestDispatcher
{
    private readonly IReadOnlyCollection<IIngestHandler> _handlers;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly TimeProvider _timeProvider;

    public IngestDispatcher(
        IEnumerable<IIngestHandler> handlers,
        IIdempotencyStore idempotencyStore,
        TimeProvider? timeProvider = null)
    {
        _handlers = handlers.ToArray();
        _idempotencyStore = idempotencyStore;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public IngestReceipt Dispatch(SourceIngestEnvelope envelope)
    {
        if (_idempotencyStore.TryGet(envelope.Source, envelope.Id, out var existing) && existing is not null)
        {
            return existing;
        }

        var handler = _handlers.FirstOrDefault(candidate => candidate.CanHandle(envelope))
            ?? throw new InvalidOperationException($"No ingest handler registered for source '{envelope.Source}' and type '{envelope.Type}'.");

        var outcome = handler.Handle(envelope);

        var receipt = new IngestReceipt
        {
            Source = envelope.Source,
            EnvelopeId = envelope.Id,
            MessageType = envelope.Type,
            ProcessedBy = handler.Name,
            CorrelationId = envelope.CorrelationId,
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Self = BuildSelfLink(envelope.Source, envelope.Id),
            Outcome = outcome
        };

        _idempotencyStore.Store(envelope.Source, envelope.Id, receipt);
        return receipt;
    }

    private static string BuildSelfLink(string source, string envelopeId)
    {
        return $"/api/v1/ingest/{Uri.EscapeDataString(source)}/{Uri.EscapeDataString(envelopeId)}";
    }
}
