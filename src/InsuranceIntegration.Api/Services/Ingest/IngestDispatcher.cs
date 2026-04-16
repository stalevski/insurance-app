using InsuranceIntegration.Api.FinalMessages.Ingest;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.Services.Ingest;

public sealed class IngestDispatcher : IIngestDispatcher
{
    private readonly IReadOnlyCollection<IIngestHandler> _handlers;
    private readonly IIdempotencyStore _idempotencyStore;

    public IngestDispatcher(IEnumerable<IIngestHandler> handlers, IIdempotencyStore idempotencyStore)
    {
        _handlers = handlers.ToArray();
        _idempotencyStore = idempotencyStore;
    }

    public IngestAcceptedResult Dispatch(SourceIngestEnvelope envelope)
    {
        if (_idempotencyStore.TryGet(envelope.Source, envelope.Id, out var existing) && existing is not null)
        {
            return existing;
        }

        var handler = _handlers.FirstOrDefault(candidate => candidate.CanHandle(envelope))
            ?? throw new InvalidOperationException($"No ingest handler registered for source '{envelope.Source}' and type '{envelope.Type}'.");

        var result = handler.Handle(envelope);

        var accepted = new IngestAcceptedResult
        {
            EnvelopeId = envelope.Id,
            Source = envelope.Source,
            Type = envelope.Type,
            HandlerName = handler.Name,
            CorrelationId = envelope.CorrelationId,
            Result = result
        };

        _idempotencyStore.Store(envelope.Source, envelope.Id, accepted);
        return accepted;
    }
}
