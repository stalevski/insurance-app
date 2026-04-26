namespace InsuranceIntegration.Api.Snapshots.Policies;

public sealed class PolicySnapshot
{
    public string PolicyReference { get; set; } = string.Empty;

    public string? QuoteReference { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public int UnderwritingYear { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public PolicyParty Insured { get; set; } = new();

    public PolicyParty Broker { get; set; } = new();

    public PolicyLifecycle Lifecycle { get; set; } = new();

    public PolicyPremium Premium { get; set; } = new();

    public PolicyCoverage Coverage { get; set; } = new();

    public PolicyDates Dates { get; set; } = new();

    public Dictionary<string, string> ExternalReferences { get; set; } = new();

    public List<SnapshotHistoryEntry> History { get; set; } = new();

    public DateTime LastUpdatedUtc { get; set; }
}

public sealed class PolicyParty
{
    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? TradingName { get; set; }
}

public sealed class PolicyLifecycle
{
    public string SubmissionStatus { get; set; } = string.Empty;

    public string QuoteStatus { get; set; } = string.Empty;

    public string PolicyStatus { get; set; } = string.Empty;

    public string ClearanceDecision { get; set; } = string.Empty;

    public bool AutoCleared { get; set; }

    public string FinalStatus { get; set; } = string.Empty;

    public string CurrentPhase { get; set; } = string.Empty;
}

public sealed class PolicyPremium
{
    public decimal? Base { get; set; }

    public decimal? Adjusted { get; set; }
}

public sealed class PolicyCoverage
{
    public int SectionCount { get; set; }

    public decimal TotalSumInsured { get; set; }

    public decimal TotalSectionPremium { get; set; }

    public bool PremiumAllocationBalanced { get; set; } = true;

    public List<string> Warnings { get; set; } = new();
}

public sealed class PolicyDates
{
    public DateOnly? InceptionDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public DateOnly? BoundDate { get; set; }
}

public sealed class SnapshotHistoryEntry
{
    public DateTime AtUtc { get; set; }

    public string Source { get; set; } = string.Empty;

    public string MessageType { get; set; } = string.Empty;

    public string EnvelopeId { get; set; } = string.Empty;

    public string TransactionType { get; set; } = string.Empty;
}
