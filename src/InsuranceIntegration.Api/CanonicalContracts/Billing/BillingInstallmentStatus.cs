namespace InsuranceIntegration.Api.CanonicalContracts.Billing;

public static class BillingInstallmentStatus
{
    public const string Planned = "Planned";

    public const string Issued = "Issued";

    public const string Paid = "Paid";

    public const string Overdue = "Overdue";

    public const string Cancelled = "Cancelled";

    public static bool IsPaid(string? status) =>
        string.Equals(status, Paid, StringComparison.OrdinalIgnoreCase);

    public static bool IsOverdue(string? status) =>
        string.Equals(status, Overdue, StringComparison.OrdinalIgnoreCase);

    public static bool IsCancelled(string? status) =>
        string.Equals(status, Cancelled, StringComparison.OrdinalIgnoreCase);

    public static bool IsOpen(string? status) =>
        !IsPaid(status) && !IsCancelled(status);
}
