using InsuranceIntegration.Api.CanonicalContracts.Claims;
using InsuranceIntegration.Api.Events;

namespace InsuranceIntegration.Api.Services.Claims;

public sealed class ClaimLifecycleService : IClaimLifecycleService
{
    // Allowed forward transitions per the claim state machine:
    // Notified -> Open
    // Open     -> Reserved | Declined | Closed
    // Reserved -> Settled | Declined | Closed
    // Settled  -> Closed
    // Declined -> Closed
    private static readonly Dictionary<string, string[]> AllowedTransitions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [ClaimStatusValue.Notified] = [ClaimStatusValue.Open],
            [ClaimStatusValue.Open] = [ClaimStatusValue.Reserved, ClaimStatusValue.Declined, ClaimStatusValue.Closed],
            [ClaimStatusValue.Reserved] = [ClaimStatusValue.Settled, ClaimStatusValue.Declined, ClaimStatusValue.Closed],
            [ClaimStatusValue.Settled] = [ClaimStatusValue.Closed],
            [ClaimStatusValue.Declined] = [ClaimStatusValue.Closed]
        };

    public ClaimTransitionResult Transition(ClaimTransitionRequest request)
    {
        if (!ClaimStatusValue.IsKnown(request.CurrentStatus))
        {
            throw new ArgumentException($"Unknown current claim status '{request.CurrentStatus}'.");
        }

        if (!ClaimStatusValue.IsKnown(request.TargetStatus))
        {
            throw new ArgumentException($"Unknown target claim status '{request.TargetStatus}'.");
        }

        if (ClaimStatusValue.IsTerminal(request.CurrentStatus))
        {
            throw new ArgumentException($"Claim is '{request.CurrentStatus}' (terminal) and cannot transition further.");
        }

        var allowed = AllowedTransitions.TryGetValue(request.CurrentStatus, out var targets)
            && targets.Any(target => string.Equals(target, request.TargetStatus, StringComparison.OrdinalIgnoreCase));

        if (!allowed)
        {
            throw new ArgumentException($"Transition from '{request.CurrentStatus}' to '{request.TargetStatus}' is not permitted.");
        }

        var canonicalTarget = ClaimStatusValue.All
            .First(value => string.Equals(value, request.TargetStatus, StringComparison.OrdinalIgnoreCase));

        if (string.Equals(canonicalTarget, ClaimStatusValue.Reserved, StringComparison.OrdinalIgnoreCase))
        {
            if (request.ReserveAmount is not { } reserve)
            {
                throw new ArgumentException("A reserve amount is required when reserving a claim.");
            }

            if (reserve < 0m)
            {
                throw new ArgumentException("Reserve amount cannot be negative.");
            }
        }

        if (string.Equals(canonicalTarget, ClaimStatusValue.Settled, StringComparison.OrdinalIgnoreCase))
        {
            if (request.SettlementAmount is not { } settlement)
            {
                throw new ArgumentException("A settlement amount is required when settling a claim.");
            }

            if (settlement < 0m)
            {
                throw new ArgumentException("Settlement amount cannot be negative.");
            }
        }

        var eventType = ResolveEventType(canonicalTarget);
        var isTerminal = ClaimStatusValue.IsTerminal(canonicalTarget);

        var reasons = new List<string>
        {
            $"Transitioned claim from {request.CurrentStatus} to {canonicalTarget}",
            $"Domain event: {eventType}"
        };

        if (request.ReserveAmount is { } reserveAmount && string.Equals(canonicalTarget, ClaimStatusValue.Reserved, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add($"Reserve set to {reserveAmount:0.##}");
        }

        if (request.SettlementAmount is { } settlementAmount && string.Equals(canonicalTarget, ClaimStatusValue.Settled, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add($"Settlement recorded at {settlementAmount:0.##}");
        }

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            reasons.Add($"Note: {request.Reason}");
        }

        return new ClaimTransitionResult
        {
            ClaimReference = request.ClaimReference,
            PolicyReference = request.PolicyReference,
            PreviousStatus = request.CurrentStatus,
            Status = canonicalTarget,
            EventType = eventType,
            IsTerminal = isTerminal,
            ReserveAmount = request.ReserveAmount,
            SettlementAmount = request.SettlementAmount,
            Reasons = reasons
        };
    }

    private static string ResolveEventType(string status)
    {
        return status switch
        {
            ClaimStatusValue.Open => DomainEventType.ClaimOpened,
            ClaimStatusValue.Reserved => DomainEventType.ClaimReserved,
            ClaimStatusValue.Settled => DomainEventType.ClaimSettled,
            ClaimStatusValue.Declined => DomainEventType.ClaimDeclined,
            ClaimStatusValue.Closed => DomainEventType.ClaimClosed,
            _ => DomainEventType.ClaimNotified
        };
    }
}
