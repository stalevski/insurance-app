using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Snapshots.Policies;
using InsuranceIntegration.Api.Snapshots.Quotes;

namespace InsuranceIntegration.Api.Services.Snapshots;

public sealed class QuoteSnapshotProjector : IQuoteSnapshotProjector
{
    public QuoteSnapshot Apply(
        QuoteSnapshot? current,
        CanonicalRiskRequest request,
        FinalRiskResponse response,
        IngestContext context)
    {
        var quoteRef = request.Quote.QuoteReference ?? request.ExternalReference;

        var snapshot = current ?? new QuoteSnapshot
        {
            QuoteReference = quoteRef
        };

        snapshot.QuoteReference = SnapshotMerge.CoalesceRequired(snapshot.QuoteReference, quoteRef);
        snapshot.PolicyReference = SnapshotMerge.Coalesce(snapshot.PolicyReference, request.Policy.PolicyReference);
        snapshot.ProductCode = SnapshotMerge.CoalesceRequired(snapshot.ProductCode, request.ProductCode);
        snapshot.UnderwritingYear = request.Submission.UnderwritingYear > 0 ? request.Submission.UnderwritingYear : snapshot.UnderwritingYear;
        snapshot.CurrencyCode = SnapshotMerge.CoalesceRequired(snapshot.CurrencyCode, request.CurrencyCode, fallback: "USD");

        SnapshotMerge.MergeInsured(snapshot.Insured, request.Insured);
        SnapshotMerge.MergeBroker(snapshot.Broker, request.Broker);

        snapshot.Lifecycle.SubmissionStatus = SnapshotMerge.CoalesceRequired(snapshot.Lifecycle.SubmissionStatus, response.SubmissionStatus);
        snapshot.Lifecycle.QuoteStatus = SnapshotMerge.CoalesceRequired(snapshot.Lifecycle.QuoteStatus, response.QuoteStatus);
        snapshot.Lifecycle.ClearanceDecision = SnapshotMerge.CoalesceRequired(snapshot.Lifecycle.ClearanceDecision, response.ClearanceDecision);
        snapshot.Lifecycle.AutoCleared = response.AutoCleared || snapshot.Lifecycle.AutoCleared;
        snapshot.Lifecycle.FinalStatus = SnapshotMerge.CoalesceRequired(snapshot.Lifecycle.FinalStatus, response.FinalStatus);
        snapshot.Lifecycle.IsBound = snapshot.Lifecycle.IsBound || !string.IsNullOrWhiteSpace(request.Policy.PolicyReference);
        snapshot.Lifecycle.CurrentPhase = SnapshotMerge.ResolveQuotePhase(response.QuoteStatus, response.SubmissionStatus, snapshot.Lifecycle.CurrentPhase, snapshot.Lifecycle.IsBound);

        if (response.BasePremium > 0m)
        {
            snapshot.Premium.Base = response.BasePremium;
        }

        if (response.AdjustedPremium > 0m)
        {
            snapshot.Premium.Adjusted = response.AdjustedPremium;
        }

        if (response.SectionCount > 0)
        {
            snapshot.Coverage.SectionCount = response.SectionCount;
            snapshot.Coverage.TotalSumInsured = response.TotalSumInsured;
            snapshot.Coverage.TotalSectionPremium = response.TotalSectionPremium;
            snapshot.Coverage.PremiumAllocationBalanced = response.PremiumAllocationBalanced;
            snapshot.Coverage.Warnings = response.CoverageWarnings.ToList();
        }

        snapshot.EffectiveDate = SnapshotMerge.Coalesce(snapshot.EffectiveDate, request.Quote.EffectiveDate);
        snapshot.ExpiryDate = SnapshotMerge.Coalesce(snapshot.ExpiryDate, request.Quote.ExpiryDate);

        if (!string.IsNullOrWhiteSpace(request.SourceSystem) && !string.IsNullOrWhiteSpace(request.ExternalReference))
        {
            snapshot.ExternalReferences[request.SourceSystem] = request.ExternalReference;
        }

        snapshot.History.Add(new SnapshotHistoryEntry
        {
            AtUtc = context.ReceivedAtUtc,
            Source = context.Source,
            MessageType = context.MessageType,
            EnvelopeId = context.EnvelopeId,
            TransactionType = request.TransactionType
        });

        snapshot.LastUpdatedUtc = context.ReceivedAtUtc;
        return snapshot;
    }
}
