namespace InsuranceIntegration.Api.CanonicalContracts.Risks;

public sealed class InstallmentData
{
    public int SequenceNumber { get; init; }

    public DateOnly DueDate { get; init; }

    public decimal Amount { get; init; }

    public bool IsPaid { get; init; }
}
