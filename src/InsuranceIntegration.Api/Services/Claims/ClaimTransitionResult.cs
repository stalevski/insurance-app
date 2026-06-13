namespace InsuranceIntegration.Api.Services.Claims;

public sealed class ClaimTransitionResult
{
    public string ClaimReference { get; init; } = string.Empty;

    public string PolicyReference { get; init; } = string.Empty;

    public string PreviousStatus { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    /// <summary>The domain event type this transition represents.</summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>True when the claim has reached a terminal status (<c>Closed</c>).</summary>
    public bool IsTerminal { get; init; }

    public decimal? ReserveAmount { get; init; }

    public decimal? SettlementAmount { get; init; }

    public List<string> Reasons { get; init; } = [];
}
