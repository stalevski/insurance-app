using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Snapshots.Policies;

namespace InsuranceIntegration.Api.Services.Snapshots;

internal static class SnapshotMerge
{
    public static string CoalesceRequired(string current, string? incoming, string fallback = "")
    {
        if (!string.IsNullOrWhiteSpace(incoming))
        {
            return incoming;
        }

        return string.IsNullOrEmpty(current) ? fallback : current;
    }

    public static string? Coalesce(string? current, string? incoming)
    {
        return !string.IsNullOrWhiteSpace(incoming) ? incoming : current;
    }

    public static DateOnly? Coalesce(DateOnly? current, DateOnly? incoming)
    {
        return incoming ?? current;
    }

    /// <summary>
    /// Indicates whether the incoming transaction actually carried a premium. The risk flow
    /// resolves the base premium from these nullable inputs and falls back to <c>0</c> when none
    /// are present, so the resolved premium alone cannot distinguish "no premium provided" from a
    /// legitimate zero (e.g. a waived policy). Projectors use this signal so an explicit zero
    /// clears any stale snapshot premium while an absent premium preserves the existing value.
    /// Mirrors the precedence in <c>RiskFlowService.ResolveBasePremium</c>.
    /// </summary>
    public static bool PremiumProvided(CanonicalRiskRequest request)
    {
        return request.Submission.BrokerPremium.HasValue
            || request.Submission.TechnicalPremium.HasValue
            || request.AnnualizedGrossPremium.HasValue;
    }

    public static void MergeInsured(PolicyParty target, InsuredData incoming)
    {
        target.Name = Coalesce(target.Name, incoming.FullName);
        target.TradingName = Coalesce(target.TradingName, incoming.TradingName);
    }

    public static void MergeBroker(PolicyParty target, BrokerData incoming)
    {
        target.Code = Coalesce(target.Code, incoming.BrokerCode);
        target.Name = Coalesce(target.Name, incoming.BrokerName);
    }

    public static string ResolvePolicyPhase(string policyStatus, string quoteStatus, string submissionStatus, string currentPhase)
    {
        if (string.Equals(policyStatus, PolicyStatusValue.Bound, StringComparison.OrdinalIgnoreCase))
        {
            return "Bound";
        }

        if (string.Equals(policyStatus, PolicyStatusValue.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            return "Cancelled";
        }

        if (string.Equals(policyStatus, PolicyStatusValue.Endorsed, StringComparison.OrdinalIgnoreCase))
        {
            return "Endorsed";
        }

        if (string.Equals(policyStatus, PolicyStatusValue.Renewed, StringComparison.OrdinalIgnoreCase))
        {
            return "Renewed";
        }

        if (string.Equals(policyStatus, PolicyStatusValue.Reinstated, StringComparison.OrdinalIgnoreCase))
        {
            return "Reinstated";
        }

        if (string.Equals(policyStatus, PolicyStatusValue.Lapsed, StringComparison.OrdinalIgnoreCase))
        {
            return "Lapsed";
        }

        if (string.Equals(policyStatus, PolicyStatusValue.NonRenewed, StringComparison.OrdinalIgnoreCase))
        {
            return "NonRenewed";
        }

        if (string.Equals(policyStatus, PolicyStatusValue.ReadyToBind, StringComparison.OrdinalIgnoreCase))
        {
            return "ReadyToBind";
        }

        if (string.Equals(quoteStatus, QuoteStatusValue.Quoted, StringComparison.OrdinalIgnoreCase))
        {
            return "Quoted";
        }

        if (!string.IsNullOrEmpty(submissionStatus))
        {
            return "Submitted";
        }

        return string.IsNullOrEmpty(currentPhase) ? "Unknown" : currentPhase;
    }

    public static string ResolveQuotePhase(string quoteStatus, string submissionStatus, string currentPhase, bool isBound = false)
    {
        if (isBound)
        {
            return "Bound";
        }

        if (string.Equals(quoteStatus, QuoteStatusValue.Quoted, StringComparison.OrdinalIgnoreCase))
        {
            return "Quoted";
        }

        if (string.Equals(quoteStatus, QuoteStatusValue.Indicative, StringComparison.OrdinalIgnoreCase))
        {
            return "Indicative";
        }

        if (string.Equals(quoteStatus, QuoteStatusValue.Blocked, StringComparison.OrdinalIgnoreCase))
        {
            return "Blocked";
        }

        if (!string.IsNullOrEmpty(submissionStatus))
        {
            return "Submitted";
        }

        return string.IsNullOrEmpty(currentPhase) ? "Unknown" : currentPhase;
    }
}
