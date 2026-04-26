using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Snapshots.Policies;

namespace InsuranceIntegration.Api.Services.Snapshots;

public sealed class PolicySnapshotProjector : IPolicySnapshotProjector
{
    public PolicySnapshot Apply(
        PolicySnapshot? current,
        CanonicalRiskRequest request,
        FinalRiskResponse response,
        IngestContext context)
    {
        var snapshot = current ?? new PolicySnapshot
        {
            PolicyReference = request.Policy.PolicyReference ?? string.Empty
        };

        snapshot.PolicyReference = SnapshotMerge.CoalesceRequired(snapshot.PolicyReference, request.Policy.PolicyReference);
        snapshot.QuoteReference = SnapshotMerge.Coalesce(snapshot.QuoteReference, request.Quote.QuoteReference);
        snapshot.ProductCode = SnapshotMerge.CoalesceRequired(snapshot.ProductCode, request.ProductCode);
        snapshot.UnderwritingYear = request.Submission.UnderwritingYear > 0 ? request.Submission.UnderwritingYear : snapshot.UnderwritingYear;
        snapshot.CurrencyCode = SnapshotMerge.CoalesceRequired(snapshot.CurrencyCode, request.CurrencyCode, fallback: "USD");

        SnapshotMerge.MergeInsured(snapshot.Insured, request.Insured);
        SnapshotMerge.MergeBroker(snapshot.Broker, request.Broker);

        snapshot.Lifecycle.SubmissionStatus = SnapshotMerge.CoalesceRequired(snapshot.Lifecycle.SubmissionStatus, response.SubmissionStatus);
        snapshot.Lifecycle.QuoteStatus = SnapshotMerge.CoalesceRequired(snapshot.Lifecycle.QuoteStatus, response.QuoteStatus);
        snapshot.Lifecycle.PolicyStatus = SnapshotMerge.CoalesceRequired(snapshot.Lifecycle.PolicyStatus, response.PolicyStatus);
        snapshot.Lifecycle.ClearanceDecision = SnapshotMerge.CoalesceRequired(snapshot.Lifecycle.ClearanceDecision, response.ClearanceDecision);
        snapshot.Lifecycle.AutoCleared = response.AutoCleared || snapshot.Lifecycle.AutoCleared;
        snapshot.Lifecycle.FinalStatus = SnapshotMerge.CoalesceRequired(snapshot.Lifecycle.FinalStatus, response.FinalStatus);
        snapshot.Lifecycle.CurrentPhase = SnapshotMerge.ResolvePolicyPhase(response.PolicyStatus, response.QuoteStatus, response.SubmissionStatus, snapshot.Lifecycle.CurrentPhase);

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

        snapshot.Dates.InceptionDate = SnapshotMerge.Coalesce(snapshot.Dates.InceptionDate, request.Quote.EffectiveDate);
        snapshot.Dates.ExpiryDate = SnapshotMerge.Coalesce(snapshot.Dates.ExpiryDate, request.Quote.ExpiryDate ?? request.Policy.ExpiryDate);
        snapshot.Dates.BoundDate = SnapshotMerge.Coalesce(snapshot.Dates.BoundDate, request.BoundDate);

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
