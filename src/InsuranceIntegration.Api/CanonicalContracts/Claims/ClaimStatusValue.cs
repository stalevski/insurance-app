namespace InsuranceIntegration.Api.CanonicalContracts.Claims;

/// <summary>
/// Lifecycle states for a claim, following the
/// <c>Notified → Open → Reserved → Settled/Declined → Closed</c> progression.
/// </summary>
public static class ClaimStatusValue
{
    public const string Notified = "Notified";
    public const string Open = "Open";
    public const string Reserved = "Reserved";
    public const string Settled = "Settled";
    public const string Declined = "Declined";
    public const string Closed = "Closed";

    public static IReadOnlyCollection<string> All { get; } =
    [
        Notified,
        Open,
        Reserved,
        Settled,
        Declined,
        Closed
    ];

    public static bool IsKnown(string status)
    {
        return All.Any(value => string.Equals(value, status, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>A terminal status admits no further transitions.</summary>
    public static bool IsTerminal(string status)
    {
        return string.Equals(status, Closed, StringComparison.OrdinalIgnoreCase);
    }
}
