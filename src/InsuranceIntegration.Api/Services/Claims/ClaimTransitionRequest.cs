namespace InsuranceIntegration.Api.Services.Claims;

/// <summary>
/// Requests a claim status transition. The service validates the move against the claim
/// state machine (<c>Notified → Open → Reserved → Settled/Declined → Closed</c>) and returns
/// the resulting status, the domain event the transition represents, and a decision narrative.
/// </summary>
public sealed class ClaimTransitionRequest
{
    public required string ClaimReference { get; init; }

    public required string PolicyReference { get; init; }

    /// <summary>The claim's current lifecycle status.</summary>
    public required string CurrentStatus { get; init; }

    /// <summary>The status the claim should transition to.</summary>
    public required string TargetStatus { get; init; }

    /// <summary>Reserve amount captured when moving to <c>Reserved</c>. Required for that transition.</summary>
    public decimal? ReserveAmount { get; init; }

    /// <summary>Indemnity amount captured when moving to <c>Settled</c>. Required for that transition.</summary>
    public decimal? SettlementAmount { get; init; }

    public string? Reason { get; init; }
}
