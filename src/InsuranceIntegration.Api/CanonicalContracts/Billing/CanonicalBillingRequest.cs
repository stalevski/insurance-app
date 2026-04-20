using System.ComponentModel.DataAnnotations;

namespace InsuranceIntegration.Api.CanonicalContracts.Billing;

public sealed class CanonicalBillingRequest
{
    public Guid EntityId { get; init; }

    [Required]
    public required string PolicyReference { get; init; }

    [Required]
    public required string SourceSystem { get; init; }

    public int InstallmentCount { get; init; }

    public decimal TotalAmount { get; init; }

    public decimal PaidToDate { get; init; }

    public int MissedPayments { get; init; }

    public string CurrencyCode { get; init; } = "USD";

    public DateOnly? FirstDueDate { get; init; }
}
