using System.ComponentModel.DataAnnotations;

namespace InsuranceIntegration.Api.SourceContracts.Billing;

public sealed class InstallmentSchedulePayload
{
    [Required]
    public required string PolicyReference { get; init; }

    [Required]
    public required int InstallmentCount { get; init; }

    [Required]
    public required decimal TotalAmount { get; init; }

    public string CurrencyCode { get; init; } = "USD";

    public DateOnly? FirstDueDate { get; init; }

    public decimal PaidToDate { get; init; }

    public int MissedPayments { get; init; }

    public List<InstallmentScheduleEntry> Installments { get; init; } = [];
}

public sealed class InstallmentScheduleEntry
{
    public int SequenceNumber { get; init; }

    public DateOnly DueDate { get; init; }

    public decimal Amount { get; init; }

    public string Status { get; init; } = "Planned";

    public DateOnly? IssuedDate { get; init; }

    public DateOnly? PaidDate { get; init; }

    public string? PaymentReference { get; init; }
}
