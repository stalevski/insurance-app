namespace InsuranceIntegration.Api.Services.Claims;

/// <summary>
/// Applies a reserve or payment operation to a claim's current financial position and recomputes
/// the incurred total (<c>incurred = paid indemnity + paid expense + outstanding reserve</c>).
/// </summary>
public sealed class ClaimFinancialRequest
{
    public required string ClaimReference { get; init; }

    public required string PolicyReference { get; init; }

    /// <summary>The operation to apply (see <see cref="ClaimFinancialOperation"/>).</summary>
    public required string Operation { get; init; }

    /// <summary>
    /// The operation amount: an absolute reserve for <c>SetReserve</c>, a signed delta for
    /// <c>AdjustReserve</c>, or a positive payment amount for the payment operations.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>Current outstanding case reserve before the operation.</summary>
    public decimal CurrentReserve { get; init; }

    /// <summary>Indemnity paid to date before the operation.</summary>
    public decimal PaidIndemnityToDate { get; init; }

    /// <summary>Expense paid to date before the operation.</summary>
    public decimal PaidExpenseToDate { get; init; }

    public string? Reason { get; init; }
}
