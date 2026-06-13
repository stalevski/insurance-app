using InsuranceIntegration.Api.CanonicalContracts.Billing;

namespace InsuranceIntegration.Api.Services.Billing;

/// <summary>
/// Assesses an installment schedule for delinquency as of a given date: open installments whose
/// due date (plus an optional grace period) has passed are flagged <see
/// cref="BillingInstallmentStatus.Overdue"/>, then the dunning and non-payment-cancellation
/// recommendation is recomputed from the updated schedule.
/// </summary>
public sealed class DelinquencyAssessmentRequest
{
    public required string PolicyReference { get; init; }

    public required string SourceSystem { get; init; }

    public string CurrencyCode { get; init; } = "USD";

    /// <summary>The installment schedule to assess.</summary>
    public required List<BillingInstallment> Installments { get; init; }

    /// <summary>Date the assessment is run for. Defaults to the current UTC date when omitted.</summary>
    public DateOnly? AsOfDate { get; init; }

    /// <summary>Days after the due date before an open installment is treated as overdue.</summary>
    public int GracePeriodDays { get; init; }
}
