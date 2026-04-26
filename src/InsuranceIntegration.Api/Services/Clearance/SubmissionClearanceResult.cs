namespace InsuranceIntegration.Api.Services.Clearance;

public sealed class SubmissionClearanceResult
{
    public string Outcome { get; init; } = SubmissionClearanceOutcome.Cleared;

    public bool IsCleared { get; init; }

    public string? DuplicateExternalReference { get; init; }

    public int BestFuzzyMatchDistance { get; init; }

    public string BestFuzzyMatchDescription { get; init; } = string.Empty;

    public List<string> Reasons { get; init; } = [];
}
