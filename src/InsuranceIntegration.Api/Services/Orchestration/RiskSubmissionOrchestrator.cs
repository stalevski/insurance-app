using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Services.Outbox;
using InsuranceIntegration.Api.Services.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Services.Orchestration;

public sealed class RiskSubmissionOrchestrator : IRiskSubmissionOrchestrator
{
    private const string AggregateTypeSubmission = "Submission";
    private const string AggregateTypeQuote = "Quote";
    private const string AggregateTypePolicy = "Policy";
    private const string EventTypeSubmissionReceived = "RiskSubmissionReceived";
    private const string EventTypeQuoteIssued = "QuoteIssued";
    private const string EventTypePolicyBound = "PolicyBound";

    private readonly IRiskFlowService _riskFlowService;
    private readonly IntegrationDbContext _context;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IRiskSnapshotRouter _snapshotRouter;
    private readonly TimeProvider _timeProvider;

    public RiskSubmissionOrchestrator(
        IRiskFlowService riskFlowService,
        IntegrationDbContext context,
        IOutboxWriter outboxWriter,
        IRiskSnapshotRouter snapshotRouter,
        TimeProvider timeProvider)
    {
        _riskFlowService = riskFlowService;
        _context = context;
        _outboxWriter = outboxWriter;
        _snapshotRouter = snapshotRouter;
        _timeProvider = timeProvider;
    }

    public async Task<FinalRiskResponse> HandleAsync(
        CanonicalRiskRequest request,
        IngestContext context,
        CancellationToken cancellationToken = default)
    {
        var response = _riskFlowService.Process(request);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var submission = await UpsertSubmissionAsync(request, response, now, context, cancellationToken);
        await UpsertQuoteAsync(request, response, submission.Id, now, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Policy.PolicyReference))
        {
            await UpsertPolicyAsync(request, response, submission.Id, now, cancellationToken);
        }

        _snapshotRouter.Route(request, response, context);

        await _context.SaveChangesAsync(cancellationToken);

        return response;
    }

    private async Task<SubmissionEntity> UpsertSubmissionAsync(
        CanonicalRiskRequest request,
        FinalRiskResponse response,
        DateTime now,
        IngestContext context,
        CancellationToken ct)
    {
        var existing = await _context.Submissions
            .FirstOrDefaultAsync(s => s.ExternalReference == request.ExternalReference, ct);

        if (existing is not null)
        {
            existing.TransactionType = request.TransactionType;
            existing.Status = response.SubmissionStatus;
            existing.ClearanceDecision = response.ClearanceDecision;
            existing.AutoCleared = response.AutoCleared;
            existing.AdjustedPremium = response.AdjustedPremium;
            existing.CorrelationId = context.CorrelationId;
            existing.UpdatedAtUtc = now;
            _outboxWriter.Enqueue(AggregateTypeSubmission, existing.Id, EventTypeSubmissionReceived, response);
            return existing;
        }

        var entity = new SubmissionEntity
        {
            Id = Guid.CreateVersion7(),
            ExternalReference = request.ExternalReference,
            ProductCode = request.ProductCode,
            SourceSystem = request.SourceSystem,
            TransactionType = request.TransactionType,
            Status = response.SubmissionStatus,
            ClearanceDecision = response.ClearanceDecision,
            AutoCleared = response.AutoCleared,
            UnderwritingYear = request.Submission.UnderwritingYear,
            BrokerCode = request.Broker.BrokerCode ?? string.Empty,
            InsuredName = request.Insured.FullName,
            AdjustedPremium = response.AdjustedPremium,
            CorrelationId = context.CorrelationId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.Submissions.Add(entity);
        _outboxWriter.Enqueue(AggregateTypeSubmission, entity.Id, EventTypeSubmissionReceived, response);
        return entity;
    }

    private async Task UpsertQuoteAsync(
        CanonicalRiskRequest request,
        FinalRiskResponse response,
        Guid submissionId,
        DateTime now,
        CancellationToken ct)
    {
        var quoteRef = !string.IsNullOrWhiteSpace(request.Quote.QuoteReference)
            ? request.Quote.QuoteReference
            : request.ExternalReference;

        if (string.IsNullOrWhiteSpace(quoteRef))
            return;

        var existing = await _context.Quotes
            .FirstOrDefaultAsync(q => q.QuoteReference == quoteRef, ct);

        if (existing is not null)
        {
            existing.Status = response.QuoteStatus;
            existing.TransactionType = request.TransactionType;
            existing.AdjustedPremium = response.AdjustedPremium;
            existing.EffectiveDate = request.Quote.EffectiveDate;
            existing.ExpiryDate = request.Quote.ExpiryDate;
            existing.UpdatedAtUtc = now;
            _outboxWriter.Enqueue(AggregateTypeQuote, existing.Id, EventTypeQuoteIssued, response);
            return;
        }

        var entity = new QuoteEntity
        {
            Id = Guid.CreateVersion7(),
            SubmissionId = submissionId,
            QuoteReference = quoteRef,
            ProductCode = request.ProductCode,
            Status = response.QuoteStatus,
            TransactionType = request.TransactionType,
            UnderwritingYear = request.Submission.UnderwritingYear,
            EffectiveDate = request.Quote.EffectiveDate,
            ExpiryDate = request.Quote.ExpiryDate,
            AdjustedPremium = response.AdjustedPremium,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.Quotes.Add(entity);
        _outboxWriter.Enqueue(AggregateTypeQuote, entity.Id, EventTypeQuoteIssued, response);
    }

    private async Task UpsertPolicyAsync(
        CanonicalRiskRequest request,
        FinalRiskResponse response,
        Guid submissionId,
        DateTime now,
        CancellationToken ct)
    {
        var policyRef = request.Policy.PolicyReference;

        var existing = await _context.Policies
            .FirstOrDefaultAsync(p => p.PolicyReference == policyRef, ct);

        if (existing is not null)
        {
            existing.Status = response.PolicyStatus;
            existing.TransactionType = request.TransactionType;
            existing.AnnualPremium = response.AdjustedPremium;
            existing.InceptionDate = request.Policy.InceptionDate;
            existing.ExpiryDate = request.Policy.ExpiryDate;
            existing.UpdatedAtUtc = now;
            _outboxWriter.Enqueue(AggregateTypePolicy, existing.Id, EventTypePolicyBound, response);
            return;
        }

        var quoteRef = !string.IsNullOrWhiteSpace(request.Quote.QuoteReference)
            ? request.Quote.QuoteReference
            : null;

        var quoteId = quoteRef is not null
            ? await _context.Quotes
                .Where(q => q.QuoteReference == quoteRef)
                .Select(q => (Guid?)q.Id)
                .FirstOrDefaultAsync(ct)
            : null;

        var entity = new PolicyEntity
        {
            Id = Guid.CreateVersion7(),
            SubmissionId = submissionId,
            QuoteId = quoteId,
            PolicyReference = policyRef,
            QuoteReference = quoteRef,
            ProductCode = request.ProductCode,
            InsuredName = request.Insured.FullName,
            Status = response.PolicyStatus,
            TransactionType = request.TransactionType,
            UnderwritingYear = request.Submission.UnderwritingYear,
            InceptionDate = request.Policy.InceptionDate,
            ExpiryDate = request.Policy.ExpiryDate,
            AnnualPremium = response.AdjustedPremium,
            PolicyVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.Policies.Add(entity);
        _outboxWriter.Enqueue(AggregateTypePolicy, entity.Id, EventTypePolicyBound, response);
    }
}
