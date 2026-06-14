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
        snapshot.PriorPolicyReference = SnapshotMerge.Coalesce(snapshot.PriorPolicyReference, request.Submission.PriorPolicyReference);
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

        if (IsQuoteIssuance(request.TransactionType, request.Policy.PolicyReference))
        {
            snapshot.Lifecycle.Version++;
            snapshot.Lifecycle.IssuedAtUtc = context.ReceivedAtUtc;
            snapshot.Lifecycle.ValidUntilUtc = context.ReceivedAtUtc.AddDays(snapshot.Lifecycle.ValidityDays);
        }

        // A successful bind clears any prior rejection reason.
        if (snapshot.Lifecycle.IsBound)
        {
            snapshot.Lifecycle.BindRejectionReason = null;
        }
        // A bind that arrived with a rejection reason on the response surfaces it.
        else if (!string.IsNullOrWhiteSpace(response.BindRejectionReason))
        {
            snapshot.Lifecycle.BindRejectionReason = response.BindRejectionReason;
        }

        // Apply the premium when the transaction carried one: either the resolved premium is
        // positive, or the request explicitly provided a premium input (which may be a legitimate
        // zero, e.g. a waived policy). An explicit zero then clears any stale value, while an
        // absent premium preserves the existing one (M5).
        if (response.BasePremium > 0m || SnapshotMerge.PremiumProvided(request))
        {
            snapshot.Premium.Base = response.BasePremium;
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

    private static bool IsQuoteIssuance(string transactionType, string? policyReference)
    {
        // A request that already carries a policy reference is a bind / post-bind lifecycle
        // event, not a (re-)issuance of the quote.
        if (!string.IsNullOrWhiteSpace(policyReference))
        {
            return false;
        }

        // Anything that's not an explicit policy-lifecycle transaction and is observed on
        // the quote aggregate is a (re-)issuance: Submission, Quote, Quoted, Quotable, ...
        return !PolicyTransactionType.IsPolicyLifecycleTransaction(transactionType)
            && !string.Equals(transactionType, QuoteTransactionType.Bind, StringComparison.OrdinalIgnoreCase);
    }
}
