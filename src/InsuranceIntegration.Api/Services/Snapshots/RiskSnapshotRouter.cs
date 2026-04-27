using System.Text.Json;
using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Events;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Events;
using InsuranceIntegration.Api.Services.Ingest;

namespace InsuranceIntegration.Api.Services.Snapshots;

public sealed class RiskSnapshotRouter : IRiskSnapshotRouter
{
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IPolicySnapshotService _policySnapshotService;
    private readonly IQuoteSnapshotService _quoteSnapshotService;
    private readonly IDomainEventLog _eventLog;
    private readonly TimeProvider _time;

    public RiskSnapshotRouter(
        IPolicySnapshotService policySnapshotService,
        IQuoteSnapshotService quoteSnapshotService,
        IDomainEventLog eventLog,
        TimeProvider time)
    {
        _policySnapshotService = policySnapshotService;
        _quoteSnapshotService = quoteSnapshotService;
        _eventLog = eventLog;
        _time = time;
    }

    public void Route(CanonicalRiskRequest request, FinalRiskResponse response, IngestContext context)
    {
        var quoteReference = !string.IsNullOrWhiteSpace(request.Quote.QuoteReference)
            ? request.Quote.QuoteReference
            : request.ExternalReference;
        var policyReference = request.Policy.PolicyReference;
        var hasQuoteReference = !string.IsNullOrWhiteSpace(quoteReference);
        var hasPolicyReference = !string.IsNullOrWhiteSpace(policyReference);

        if (!hasQuoteReference && !hasPolicyReference)
        {
            return;
        }

        var payloadJson = JsonSerializer.Serialize(new RiskEventPayload(request, response), PayloadSerializerOptions);
        var recordedAtUtc = _time.GetUtcNow().UtcDateTime;

        if (hasQuoteReference)
        {
            _eventLog.Append(BuildEvent(
                eventType: ResolveQuoteEventType(request.TransactionType, hasPolicyReference),
                aggregateKind: DomainEventAggregateKind.Quote,
                aggregateKey: quoteReference!,
                context: context,
                payloadJson: payloadJson,
                recordedAtUtc: recordedAtUtc));
            _quoteSnapshotService.Apply(request, response, context);
        }

        if (hasPolicyReference)
        {
            _eventLog.Append(BuildEvent(
                eventType: ResolvePolicyEventType(request.TransactionType),
                aggregateKind: DomainEventAggregateKind.Policy,
                aggregateKey: policyReference!,
                context: context,
                payloadJson: payloadJson,
                recordedAtUtc: recordedAtUtc));
            _policySnapshotService.Apply(request, response, context);
        }
    }

    private static DomainEventEntity BuildEvent(
        string eventType,
        string aggregateKind,
        string aggregateKey,
        IngestContext context,
        string payloadJson,
        DateTime recordedAtUtc)
    {
        return new DomainEventEntity
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            AggregateKind = aggregateKind,
            AggregateKey = aggregateKey,
            Source = context.Source,
            EnvelopeId = context.EnvelopeId,
            CorrelationId = context.CorrelationId,
            OccurredAtUtc = context.ReceivedAtUtc,
            RecordedAtUtc = recordedAtUtc,
            PayloadJson = payloadJson
        };
    }

    private static string ResolveQuoteEventType(string transactionType, bool hasPolicyReference)
    {
        // A canonical request that brings a policy reference is binding the quote into a policy.
        if (IsBindTransaction(transactionType) || hasPolicyReference)
        {
            return DomainEventType.QuoteBound;
        }

        // Submission-flavored transactions land first as RiskSubmissionReceived.
        if (SubmissionTransactionType.IsSubmissionTransaction(transactionType))
        {
            return DomainEventType.RiskSubmissionReceived;
        }

        // Anything else affecting a quote (Quote/Quoted/Quotable/...) is treated as a quote issuance.
        return DomainEventType.QuoteIssued;
    }

    private static string ResolvePolicyEventType(string transactionType)
    {
        if (string.Equals(transactionType, PolicyTransactionType.Cancellation, StringComparison.OrdinalIgnoreCase))
        {
            return DomainEventType.PolicyCancelled;
        }

        if (string.Equals(transactionType, PolicyTransactionType.MidTermAdjustment, StringComparison.OrdinalIgnoreCase))
        {
            return DomainEventType.PolicyEndorsed;
        }

        if (string.Equals(transactionType, PolicyTransactionType.Renewal, StringComparison.OrdinalIgnoreCase))
        {
            return DomainEventType.PolicyRenewed;
        }

        if (string.Equals(transactionType, PolicyTransactionType.Reinstatement, StringComparison.OrdinalIgnoreCase))
        {
            return DomainEventType.PolicyReinstated;
        }

        return DomainEventType.PolicyBound;
    }

    private static bool IsBindTransaction(string transactionType)
    {
        return string.Equals(transactionType, "PolicyBind", StringComparison.OrdinalIgnoreCase)
            || string.Equals(transactionType, QuoteTransactionType.Bind, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record RiskEventPayload(CanonicalRiskRequest CanonicalRequest, FinalRiskResponse FinalResponse);
}
