namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public static class PolicyTransactionType
{
    public const string NewBusiness = "NewBusiness";

    public const string Renewal = "Renewal";

    public const string MidTermAdjustment = "MidTermAdjustment";

    public const string Cancellation = "Cancellation";

    public const string Reinstatement = "Reinstatement";

    public static IReadOnlyCollection<string> All { get; } =
    [
        NewBusiness,
        Renewal,
        MidTermAdjustment,
        Cancellation,
        Reinstatement
    ];

    public static bool IsPolicyTransaction(string transactionType)
    {
        return All.Any(value => string.Equals(value, transactionType, StringComparison.OrdinalIgnoreCase));
    }
}
