using InsuranceIntegration.Api.CanonicalContracts.Billing;

namespace InsuranceIntegration.Api.IntegrationTests.Builders;

/// <summary>
/// Builds installment schedules for the billing endpoints. Each status string produces one
/// quarterly installment of a fixed amount, numbered in sequence from one.
/// </summary>
public static class BillingScheduleBuilder
{
    /// <summary>Builds a schedule with one installment per supplied status, each due a quarter apart.</summary>
    public static List<BillingInstallment> WithStatuses(decimal amount, params string[] statuses)
    {
        return statuses
            .Select((status, index) => new BillingInstallment
            {
                SequenceNumber = index + 1,
                DueDate = new DateOnly(2026, 1, 1).AddMonths(index * 3),
                Amount = amount,
                Status = status,
            })
            .ToList();
    }
}
