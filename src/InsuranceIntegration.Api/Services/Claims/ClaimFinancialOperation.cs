namespace InsuranceIntegration.Api.Services.Claims;

/// <summary>
/// Financial operations that can be applied to a claim's reserve and payment position.
/// </summary>
public static class ClaimFinancialOperation
{
    /// <summary>Replace the case reserve with an absolute amount.</summary>
    public const string SetReserve = "SetReserve";

    /// <summary>Increase (positive) or decrease (negative) the case reserve by a delta.</summary>
    public const string AdjustReserve = "AdjustReserve";

    /// <summary>Record an indemnity (loss) payment; draws the reserve down by the same amount.</summary>
    public const string RecordIndemnityPayment = "RecordIndemnityPayment";

    /// <summary>Record an expense (e.g. legal / adjuster) payment; does not draw down the reserve.</summary>
    public const string RecordExpensePayment = "RecordExpensePayment";

    public static IReadOnlyCollection<string> All { get; } =
    [
        SetReserve,
        AdjustReserve,
        RecordIndemnityPayment,
        RecordExpensePayment
    ];

    public static bool IsKnown(string operation)
    {
        return All.Any(value => string.Equals(value, operation, StringComparison.OrdinalIgnoreCase));
    }
}
