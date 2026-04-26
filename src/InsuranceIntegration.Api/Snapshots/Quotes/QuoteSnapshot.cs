using InsuranceIntegration.Api.Snapshots.Policies;

namespace InsuranceIntegration.Api.Snapshots.Quotes;

public sealed class QuoteSnapshot
{
    public string QuoteReference { get; set; } = string.Empty;

    public string? PolicyReference { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public int UnderwritingYear { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public PolicyParty Insured { get; set; } = new();

    public PolicyParty Broker { get; set; } = new();

    public QuoteLifecycle Lifecycle { get; set; } = new();

    public PolicyPremium Premium { get; set; } = new();

    public PolicyCoverage Coverage { get; set; } = new();

    public DateOnly? EffectiveDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public Dictionary<string, string> ExternalReferences { get; set; } = new();

    public List<SnapshotHistoryEntry> History { get; set; } = new();

    public DateTime LastUpdatedUtc { get; set; }
}

public sealed class QuoteLifecycle
{
    public string SubmissionStatus { get; set; } = string.Empty;

    public string QuoteStatus { get; set; } = string.Empty;

    public string ClearanceDecision { get; set; } = string.Empty;

    public bool AutoCleared { get; set; }

    public string FinalStatus { get; set; } = string.Empty;

    public string CurrentPhase { get; set; } = string.Empty;

    public bool IsBound { get; set; }
}
