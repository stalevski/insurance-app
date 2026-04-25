namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public static class SectionStatus
{
    public const string Active = "Active";

    public const string Suspended = "Suspended";

    public const string Removed = "Removed";

    public static bool IsActive(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            || string.Equals(status, Active, StringComparison.OrdinalIgnoreCase);
    }
}
