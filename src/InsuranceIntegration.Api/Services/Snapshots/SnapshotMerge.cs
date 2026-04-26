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
