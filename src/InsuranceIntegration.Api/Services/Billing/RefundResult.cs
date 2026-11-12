namespace InsuranceIntegration.Api.Services.Billing;

public sealed class RefundResult
{
    public string PolicyReference { get; init; } = string.Empty;

    public string TransactionType { get; init; } = string.Empty;

    public string TransactionReference { get; init; } = string.Empty;

    /// <summary>
    /// Deterministic identifier of the refund (<c>RFD-CAN-&lt;transactionReference&gt;</c> or
    /// <c>RFD-END-&lt;transactionReference&gt;</c>). Replaying the same adjustment yields the same
    /// reference, so a downstream ledger can reject the duplicate instead of paying twice.
    /// </summary>
    public string RefundReference { get; init; } = string.Empty;

    /// <summary>Return premium requested by the adjustment.</summary>
    public decimal ReturnPremium { get; init; }

    /// <summary>Part of the return premium the insured never paid, so it cannot be refunded.</summary>
    public decimal UncollectedPortion { get; init; }

    /// <summary>Return premium after capping it to the premium in hand.</summary>
    public decimal RefundableBase { get; init; }

    /// <summary>Admin fee actually withheld (capped at the refundable base).</summary>
    public decimal AdminFeeWithheld { get; init; }

    /// <summary>Arrears actually recovered from the refund.</summary>
    public decimal OffsetAgainstArrears { get; init; }

    /// <summary>Amount payable to the insured, rounded to two decimals.</summary>
    public decimal NetRefundAmount { get; init; }

    /// <summary><see cref="RefundOutcome"/>: <c>Payable</c>, <c>NoRefund</c> or <c>Exhausted</c>.</summary>
    public string Outcome { get; init; } = RefundOutcome.NoRefund;

    public string CurrencyCode { get; init; } = "USD";

    public string PayoutMethod { get; init; } = RefundPayoutMethod.BankTransfer;

    /// <summary>Effective date plus the channel's clearing lead time.</summary>
    public DateOnly PayableDate { get; init; }

    /// <summary>True when the return premium exceeded the premium in hand and had to be capped.</summary>
    public bool CappedByCollectedPremium { get; init; }

    public List<string> Reasons { get; init; } = [];
}
