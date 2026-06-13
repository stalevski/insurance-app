namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public static class PolicyTransactionType
{
    public const string NewBusiness = "NewBusiness";

    public const string Renewal = "Renewal";

    public const string MidTermAdjustment = "MidTermAdjustment";

    public const string Cancellation = "Cancellation";

    public const string Reinstatement = "Reinstatement";

    public const string Lapse = "Lapse";

    public const string NonRenewal = "NonRenewal";

    public static IReadOnlyCollection<string> All { get; } =
    [
        NewBusiness,
        Renewal,
        MidTermAdjustment,
        Cancellation,
        Reinstatement,
        Lapse,
        NonRenewal
    ];

    public static bool IsPolicyTransaction(string transactionType)
    {
        return All.Any(value => string.Equals(value, transactionType, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Post-bind policy lifecycle operations. These do not re-quote and should not write
    /// a quote-aggregate event when routed through the snapshot router.
    /// </summary>
    public static bool IsPolicyLifecycleTransaction(string transactionType)
    {
        return string.Equals(transactionType, Cancellation, StringComparison.OrdinalIgnoreCase)
            || string.Equals(transactionType, MidTermAdjustment, StringComparison.OrdinalIgnoreCase)
            || string.Equals(transactionType, Renewal, StringComparison.OrdinalIgnoreCase)
            || string.Equals(transactionType, Reinstatement, StringComparison.OrdinalIgnoreCase)
            || string.Equals(transactionType, Lapse, StringComparison.OrdinalIgnoreCase)
            || string.Equals(transactionType, NonRenewal, StringComparison.OrdinalIgnoreCase);
    }
}
