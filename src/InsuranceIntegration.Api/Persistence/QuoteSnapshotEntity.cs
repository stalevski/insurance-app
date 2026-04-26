namespace InsuranceIntegration.Api.Persistence;

public sealed class QuoteSnapshotEntity
{
    public string QuoteReference { get; set; } = string.Empty;

    public string? PolicyReference { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public int UnderwritingYear { get; set; }

    public string CurrentPhase { get; set; } = string.Empty;

    public bool IsBound { get; set; }

    public string SnapshotJson { get; set; } = string.Empty;

    public DateTime LastUpdatedUtc { get; set; }
}
