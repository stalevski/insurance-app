namespace InsuranceIntegration.Api.CanonicalContracts.Billing;

public sealed class BillingInstallment
{
    public int SequenceNumber { get; init; }

    public DateOnly DueDate { get; init; }

    public decimal Amount { get; init; }

    public string Status { get; init; } = BillingInstallmentStatus.Planned;

    public DateOnly? IssuedDate { get; init; }

    public DateOnly? PaidDate { get; init; }

    public string? PaymentReference { get; init; }
}
