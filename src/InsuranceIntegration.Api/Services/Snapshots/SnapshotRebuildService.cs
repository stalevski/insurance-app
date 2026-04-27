using System.Text.Json;
using InsuranceIntegration.Api.Events;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Events;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Snapshots.Policies;
using InsuranceIntegration.Api.Snapshots.Quotes;

namespace InsuranceIntegration.Api.Services.Snapshots;

public sealed class SnapshotRebuildService : ISnapshotRebuildService
{
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDomainEventLog _eventLog;
    private readonly IPolicySnapshotProjector _policyProjector;
    private readonly IQuoteSnapshotProjector _quoteProjector;

    public SnapshotRebuildService(
        IDomainEventLog eventLog,
        IPolicySnapshotProjector policyProjector,
        IQuoteSnapshotProjector quoteProjector)
    {
        _eventLog = eventLog;
        _policyProjector = policyProjector;
        _quoteProjector = quoteProjector;
    }

    public SnapshotRebuildResult<PolicySnapshot> RebuildPolicy(string policyReference)
    {
        var events = _eventLog.GetByAggregate(DomainEventAggregateKind.Policy, policyReference);
        PolicySnapshot? snapshot = null;
        foreach (var entity in events)
        {
            var payload = DeserializePayload(entity);
            if (payload is null)
            {
                continue;
            }
            var context = ToIngestContext(entity);
            snapshot = _policyProjector.Apply(snapshot, payload.CanonicalRequest, payload.FinalResponse, context);
        }

        return new SnapshotRebuildResult<PolicySnapshot>
        {
            AggregateKind = DomainEventAggregateKind.Policy,
            AggregateKey = policyReference,
            EventsApplied = events.Count,
            FirstEventAtUtc = events.FirstOrDefault()?.OccurredAtUtc,
            LastEventAtUtc = events.LastOrDefault()?.OccurredAtUtc,
            Snapshot = snapshot
        };
    }

    public SnapshotRebuildResult<QuoteSnapshot> RebuildQuote(string quoteReference)
    {
        var events = _eventLog.GetByAggregate(DomainEventAggregateKind.Quote, quoteReference);
        QuoteSnapshot? snapshot = null;
        foreach (var entity in events)
        {
            var payload = DeserializePayload(entity);
            if (payload is null)
            {
                continue;
            }
            var context = ToIngestContext(entity);
            snapshot = _quoteProjector.Apply(snapshot, payload.CanonicalRequest, payload.FinalResponse, context);
        }

        return new SnapshotRebuildResult<QuoteSnapshot>
        {
            AggregateKind = DomainEventAggregateKind.Quote,
            AggregateKey = quoteReference,
            EventsApplied = events.Count,
            FirstEventAtUtc = events.FirstOrDefault()?.OccurredAtUtc,
            LastEventAtUtc = events.LastOrDefault()?.OccurredAtUtc,
            Snapshot = snapshot
        };
    }

    private static RiskEventPayload? DeserializePayload(DomainEventEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.PayloadJson))
        {
            return null;
        }
        return JsonSerializer.Deserialize<RiskEventPayload>(entity.PayloadJson, PayloadSerializerOptions);
    }

    private static IngestContext ToIngestContext(DomainEventEntity entity)
    {
        return new IngestContext
        {
            Source = entity.Source,
            EnvelopeId = entity.EnvelopeId ?? entity.Id.ToString(),
            MessageType = entity.EventType,
            ReceivedAtUtc = entity.OccurredAtUtc,
            CorrelationId = entity.CorrelationId
        };
    }
}
