namespace InsuranceIntegration.Api.Persistence;

public sealed class PolicySnapshotEntity
{
    public string PolicyReference { get; set; } = string.Empty;

    public string? QuoteReference { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public int UnderwritingYear { get; set; }

    public string CurrentPhase { get; set; } = string.Empty;

    public string SnapshotJson { get; set; } = string.Empty;

    public DateTime LastUpdatedUtc { get; set; }
}
