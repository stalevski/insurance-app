using InsuranceIntegration.Api.CanonicalContracts.Billing;

namespace InsuranceIntegration.Api.Services.Billing;

/// <summary>
/// Records a payment against an existing installment schedule. The payment settles open
/// installments in due order (a specific installment may be targeted); each fully-covered
/// installment transitions to <see cref="BillingInstallmentStatus.Paid"/> and the billing
/// position (outstanding balance, next due date, delinquency) is recomputed.
/// </summary>
public sealed class PaymentRecordRequest
{
    public required string PolicyReference { get; init; }

    public required string SourceSystem { get; init; }

    public string CurrencyCode { get; init; } = "USD";

    /// <summary>The current installment schedule the payment is applied against.</summary>
    public required List<BillingInstallment> Installments { get; init; }

    /// <summary>Amount received from the insured.</summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Optional specific installment to start settling from. When null, the payment is applied to
    /// the earliest open (unpaid, non-cancelled) installment and forward, in due order.
    /// </summary>
    public int? InstallmentNumber { get; init; }

    /// <summary>Date the payment was received. Defaults to the current UTC date when omitted.</summary>
    public DateOnly? PaidDate { get; init; }

    public string? PaymentReference { get; init; }
}
