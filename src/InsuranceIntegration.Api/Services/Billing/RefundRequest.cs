namespace InsuranceIntegration.Api.Services.Billing;

/// <summary>
/// Turns the return premium produced by a cancellation or endorsement
/// (<c>Services/Policies/PolicyAdjustmentService</c>) into a payable refund decision.
/// <para>
/// The policy adjustment services decide <em>how much</em> premium is unearned; this request
/// carries that figure plus the money already in hand, so the refund can be capped by what was
/// actually collected, reduced by the admin fee, and offset against arrears.
/// </para>
/// </summary>
public sealed class RefundRequest
{
    public required string PolicyReference { get; init; }

    /// <summary>Which adjustment produced the return premium: <c>Cancellation</c> or <c>Endorsement</c>.</summary>
    public required string TransactionType { get; init; }

    /// <summary>
    /// Identifier of the originating adjustment. Together with the policy reference it makes the
    /// refund reference deterministic, so replaying the same adjustment cannot produce a second
    /// payable refund.
    /// </summary>
    public required string TransactionReference { get; init; }

    /// <summary>Return premium as calculated by the policy adjustment service (never negative).</summary>
    public required decimal ReturnPremium { get; init; }

    /// <summary>Premium actually received from the insured to date.</summary>
    public decimal CollectedPremium { get; init; }

    /// <summary>Premium already refunded on this policy, across earlier transactions.</summary>
    public decimal PreviouslyRefunded { get; init; }

    /// <summary>Administrative fee withheld from the return premium (cancellation/endorsement fee).</summary>
    public decimal AdminFeeWithheld { get; init; }

    /// <summary>Unpaid premium still owed by the insured; arrears are recovered from the refund first.</summary>
    public decimal OutstandingPremium { get; init; }

    public string CurrencyCode { get; init; } = "USD";

    /// <summary><c>BankTransfer</c> (default), <c>Card</c> or <c>Credit</c>.</summary>
    public string PayoutMethod { get; init; } = RefundPayoutMethod.BankTransfer;

    /// <summary>Date the adjustment takes effect. Defaults to the current UTC date when omitted.</summary>
    public DateOnly? EffectiveDate { get; init; }
}
